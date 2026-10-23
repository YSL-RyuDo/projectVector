#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit local QA only. Uses real client/server handlers and authored UI;
// does not fabricate responses, create UI, or store credentials.
public sealed partial class RankedCaptureTest : MonoBehaviour
{
    static readonly string[] Accounts = { "qwer", "asdf", "zxcv", "wert", "sdfg", "xcvb" };
    string account, root, frames;
    bool recording, finished;
    int frame;
    int encounterRound = 1;
    bool Lifecycle => Environment.GetEnvironmentVariable("VECTOR_CAPTURE_LIFECYCLE") == "1";
    bool DroneEvidence => Environment.GetEnvironmentVariable("VECTOR_CAPTURE_DRONE") == "1";
    StreamWriter timeline;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);
    static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Private).Invoke(obj, args);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Environment.GetEnvironmentVariable("VECTOR_CAPTURE_OPT_IN") != "local-ranked-qa") return;
        if (FindFirstObjectByType<RankedCaptureTest>() != null) return;
        string user = Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ACCOUNT");
        string output = Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT");
        if (!Accounts.Contains(user) || string.IsNullOrEmpty(output) || !Directory.Exists(output)) return;
        var go = new GameObject("OptInRankedCaptureTest");
        DontDestroyOnLoad(go);
        var runner = go.AddComponent<RankedCaptureTest>(); runner.account = user; runner.root = output;
        runner.StartCoroutine(runner.Run()); runner.StartCoroutine(runner.Watchdog());
    }

    void Stage(string value)
    {
        Debug.Log("[RANKED CAPTURE] " + account + " " + value);
        File.WriteAllText(Path.Combine(root, account + ".status"), value);
    }
    bool All(string suffix) => Accounts.All(a => File.Exists(Path.Combine(root, a + suffix)));
    IEnumerator Wait(Func<bool> condition, string stage, float seconds = 90)
    {
        Stage(stage);
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end) { Finish("FAILED: timeout " + stage); yield break; }
            yield return null;
        }
    }
    IEnumerator Watchdog()
    {
        yield return new WaitForSecondsRealtime(420);
        if (!finished) Finish("FAILED: overall timeout");
    }
    IEnumerator Run()
    {
        Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = 30;
        if (!Application.isEditor) Screen.SetResolution(960, 540, false);
        yield return Wait(() => NetworkManager.Instance != null && NetworkManager.Instance.IsConnected, "connecting");
        if (finished) yield break;
        var net = NetworkManager.Instance;
        string password = Environment.GetEnvironmentVariable("VECTOR_CAPTURE_PASSWORD");
        Environment.SetEnvironmentVariable("VECTOR_CAPTURE_PASSWORD", null);
        if (string.IsNullOrEmpty(password)) { Finish("FAILED: missing temporary credential"); yield break; }
        net.SendLogin(account, password); password = null;
        yield return Wait(() => net.IsAuthenticated, "authenticating", 15);
        if (finished) yield break;
        FindFirstObjectByType<LoginUI>().OnClickEnter();
        yield return Wait(() => SceneManager.GetActiveScene().name == "Lobby" && net.MyPlayerId != 0 && LobbySocialController.Instance != null, "entering lobby");
        if (finished) yield break;
        yield return new WaitForSecondsRealtime(2);
        net.SendFriendPresenceSet(FriendPresenceMode.Online);
        File.WriteAllText(Path.Combine(root, account + ".id"), net.MyPlayerId.ToString());
        yield return Wait(() => All(".id"), "waiting for six authenticated clients");
        if (finished) yield break;
        var game = FindFirstObjectByType<GamePanelController>(FindObjectsInactive.Include);
        game.OpenPartyLobby();
        int index = Array.IndexOf(Accounts, account);
        bool leader = index == 0 || index == 2;
        bool member = index == 1 || index == 3;
        if (leader)
        {
            string peer = Accounts[index + 1];
            net.SendFriendOperation(FriendOperation.SendRequest, peer);
            File.WriteAllText(Path.Combine(root, account + ".friend-requested"), "sent");
            yield return new WaitForSecondsRealtime(4);
            net.SendSocial(PacketType.PKT_CS_PARTY_INVITE, uint.Parse(File.ReadAllText(Path.Combine(root, peer + ".id"))));
        }
        else if (member)
        {
            string inviter = Accounts[index - 1];
            yield return Wait(() => File.Exists(Path.Combine(root, inviter + ".friend-requested")), "waiting for friend request");
            if (finished) yield break;
            yield return new WaitForSecondsRealtime(1);
            net.SendFriendOperation(FriendOperation.AcceptRequest, inviter);
            yield return Wait(() => LobbySocialController.Instance.invitePanel.activeSelf, "waiting for party invitation", 20);
            if (finished) yield break;
            yield return new WaitForSecondsRealtime(1);
            LobbySocialController.Instance.acceptInvite.onClick.Invoke();
        }
        if (leader || member)
        {
            yield return Wait(() => net.PartyPlayers.Count == 2 && net.PartyReadyToQueue, "forming duo", 20);
            if (finished) yield break;
        }
        if (!member) game.OnClickRankGameButton();
        yield return new WaitForSecondsRealtime(2);
        File.WriteAllText(Path.Combine(root, account + ".prepared"), "ranked");
        yield return Wait(() => All(".prepared"), "waiting for both duos and solos");
        if (finished) yield break;
        if (account == "qwer") File.WriteAllText(Path.Combine(root, "start.utc"), DateTimeOffset.UtcNow.AddSeconds(4).ToUnixTimeMilliseconds().ToString());
        yield return Wait(() => File.Exists(Path.Combine(root, "start.utc")), "waiting for recording start");
        if (finished) yield break;
        // Writer completes before this delay; never read a partially written control file.
        yield return new WaitForSecondsRealtime(.5f);
        long start = long.Parse(File.ReadAllText(Path.Combine(root, "start.utc")));
        while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < start) yield return null;
        frames = Path.Combine(root, account + "-frames"); Directory.CreateDirectory(frames);
        timeline = new StreamWriter(Path.Combine(frames, "timeline.csv")); timeline.WriteLine("frame,utcMilliseconds");
        recording = true; StartCoroutine(Capture()); Stage("recording ranked lobby");
        yield return new WaitForSecondsRealtime(6);
        if (!member) game.OnClickTempGameStart();
        yield return Wait(() => LobbySocialController.Instance != null && LobbySocialController.Instance.readyPanel.activeSelf, "ranked matchmaking", 90);
        if (finished) yield break;
        yield return new WaitForSecondsRealtime(3 + index * .4f);
        LobbySocialController.Instance.acceptReady.onClick.Invoke();
        yield return Wait(() => PickManager.Instance != null && Field<PickStateData>(PickManager.Instance, "currentState") != null, "loading character selection");
        if (finished) yield break;
        var pick = PickManager.Instance;
        if (net.ReadyCardAppearances.Count != 6 || !Field<MapIntroPresentation>(pick, "mapIntro").IsPlaying)
        { Finish("FAILED: six accepted card identities or pre-pick map introduction missing"); yield break; }
        Stage("verified six accepted card identities and actual map introduction");
        yield return Wait(() => !Field<bool>(pick, "isInputLockedByPhase") &&
            Field<PickStateData>(pick, "currentState").members.Any(m => m.isMe), "waiting for character input to unlock", 15);
        if (finished) yield break;
        var state = Field<PickStateData>(pick, "currentState");
        var members = state.members.OrderBy(m => m.playerId).ToList();
        int position = members.FindIndex(m => m.isMe);
        var characters = Field<PickCharacterData[]>(pick, "characters");
        int character = characters[Mathf.Max(position, 0) % characters.Length].characterId;
        if (SentinelEvidence) {
            uint pilot=uint.Parse(File.ReadAllText(Path.Combine(root,"qwer.id")));
            bool withPilot=members.Any(m=>m.playerId==pilot);
            character=withPilot ? (account=="qwer"?2:members.Where(m=>m.playerId!=pilot).ToList().FindIndex(m=>m.isMe)) : position;
        }
        if (EntryEvidence) {
            uint pilot=uint.Parse(File.ReadAllText(Path.Combine(root,"qwer.id")));
            bool withPilot=members.Any(m=>m.playerId==pilot);
            character=withPilot ? (account=="qwer"?3:members.Where(m=>m.playerId!=pilot).ToList().FindIndex(m=>m.isMe)) : position;
        }
        if (DroneEvidence) {
            uint pilot=uint.Parse(File.ReadAllText(Path.Combine(root,"qwer.id")));
            int pilotIndex=members.FindIndex(m=>m.playerId==pilot);
            character=pilotIndex>=0 ? (account=="qwer"?0:1+members.Where(m=>m.playerId!=pilot).ToList().FindIndex(m=>m.isMe)) : position;
        }
        File.WriteAllText(Path.Combine(root, account + ".team"), "room=" + state.roomId + " team=" + state.myTeam + " character=" + character + " members=" + string.Join(",", members.Select(m => m.playerId)));
        yield return new WaitForSecondsRealtime(1);
        Call(pick, "OnClickCharacter", character); Stage("preview character " + character);
        yield return Wait(() => Field<PickStateData>(pick, "currentState").members.Any(m => m.isMe && m.hoverCharacterId == character), "server confirms character preview", 5);
        if (finished) yield break;
        yield return new WaitForSecondsRealtime(2);
        Call(pick, "SelectSkill", 1);
        yield return new WaitForSecondsRealtime(2);
        Call(pick, "OnClickSelect"); Stage("confirm character " + character);
        yield return Wait(() => Field<PickStateData>(pick, "currentState").members.Any(m => m.isMe && m.isLocked && m.lockedCharacterId == character), "server confirms selected character", 5);
        if (finished) yield break;
        File.WriteAllText(Path.Combine(root, account + ".selected"), character.ToString());
        yield return Wait(() => GameController.Instance != null && GameController.Instance.CurrentState == GameController.GameFlowState.Playing, "loading actual match", 90);
        if (finished) yield break;
        Stage("in game"); File.WriteAllText(Path.Combine(root, account + ".playing"), "verified");
        if ((SentinelEvidence || EntryEvidence) && account=="qwer") {
            foreach(var slot in new[]{SkillSlot.C,SkillSlot.Q,SkillSlot.E,SkillSlot.X})net.SendStoreBuyRequest(2,(int)slot);
        }
        if ((Lifecycle || DroneEvidence) && !DischargeEvidence && net.MyPlayer != null && net.MyPlayer.GetComponent<InitiatorSkill_Q>() != null)
            net.SendStoreBuyRequest(2, (int)SkillSlot.Q);
        if (DischargeEvidence && net.MyPlayer?.GetComponent<InitiatorSkill_X>() != null)
            net.SendStoreBuyRequest(2, (int)SkillSlot.X);
        if (DroneEvidence && Environment.GetEnvironmentVariable("VECTOR_CAPTURE_EMP")=="1" && net.MyPlayer?.GetComponent<InitiatorSkill_E>()!=null)
            net.SendStoreBuyRequest(2,(int)SkillSlot.E);
        if (DroneEvidence && Environment.GetEnvironmentVariable("VECTOR_CAPTURE_SCANNER")=="1" && net.MyPlayer?.GetComponent<InitiatorSkill_C>()!=null)
            net.SendStoreBuyRequest(2,(int)SkillSlot.C);
        yield return Wait(() => RoundManager.Instance != null && RoundManager.Instance.currentState == RoundManager.RoundState.MainRound && net.MyPlayer != null,
            "waiting for combat round", 40);
        if (finished) yield break;
        if (DroneEvidence) { yield return CaptureDroneEncounter(); if(!finished) Finish(DischargeEvidence ? "PASS: actual X input, server discharge hit and first-person weapon recovery" : Environment.GetEnvironmentVariable("VECTOR_CAPTURE_SCANNER")=="1" ? "PASS: scanner throw, authoritative shock, weapon restoration and cleanup" : Environment.GetEnvironmentVariable("VECTOR_CAPTURE_DRONE_CANCEL")=="1" ? "PASS: queued deploy cancellation and weapon input lock" : "PASS: actual drone flight, taser victim status and enemy weapon destruction"); yield break; }
        if (SentinelEvidence) { yield return CaptureSentinelEncounter(); if(!finished)Finish("PASS: live Sentinel inputs, server detection/status/damage and client movement/recovery"); yield break; }
        if (EntryEvidence) { yield return CaptureEntryEncounter(); if(!finished)Finish(EntryDashOnly ? "PASS: live Entry C, opponent path continuity, endpoint agreement, firearm recovery and normal movement" : "PASS: live Entry C Q E X, replicated effects, health, ammunition and firearm recovery"); yield break; }
        if (Environment.GetEnvironmentVariable("VECTOR_CAPTURE_OBJECTIVE") == "1")
        {
            yield return ProbeObjective();
            if(!finished) Finish("PASS: six clients observe valid plant, ESC cancel, exclusive defuse, range cancel and server round result");
            yield break;
        }
        if (Lifecycle) { yield return ProbeActions(); if(finished) yield break; }
        if (Environment.GetEnvironmentVariable("VECTOR_CAPTURE_RECONNECT") == "1")
        {
            yield return ReconnectEncounter();
            if (finished) yield break;
        }
        yield return StageCombat();
        if (finished) yield break;
        if (Lifecycle)
        {
            yield return Wait(()=>RoundManager.Instance.currentState==RoundManager.RoundState.RoundEnd,"first round ended",45);
            if(finished) yield break;
            SettingsMenu.Instance.OpenSettingsMenu();
            yield return Wait(()=>RoundManager.Instance.currentState==RoundManager.RoundState.Preparation,"ESC open across round reset and side swap",15);
            if(finished) yield break;
            var me=net.MyPlayer;
            var info=FindFirstObjectByType<MapInfo>();
            var expected=info.GetSpawnPoint((byte)(1-me.Team),me.SlotIndex).position;
            if(me.GetComponent<PlayerHP>().IsDead || Vector3.Distance(me.transform.position,expected)>1.5f ||
                InitiatorSkill_Q.IsLocalDroneMode || EntrySkill_X.IsLocalBladeMode || !SettingsMenu.Instance.IsOpen ||
                Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            { Finish("FAILED: round reset under ESC/side swap mismatch"); yield break; }
            Stage("PASS alive/dead reset and opposite-side spawn while ESC remains open");
            File.WriteAllText(Path.Combine(root,account+".round-reset"),"PASS: alive/dead reset, opposite side spawn under ESC");
            SettingsMenu.Instance.CloseSettingsMenu();
            me.GetComponent<PlayerMovement>().enabled=true;
            me.GetComponent<PlayerCameraController>().CameraLook.enabled=true;
            yield return Wait(()=>RoundManager.Instance.currentState==RoundManager.RoundState.MainRound,"second round buy end",20);
            if(finished) yield break;
            if(info.barriers.activeSelf || !RoundManager.Instance.CanAttack)
            { Finish("FAILED: buy-end barriers/attack"); yield break; }
            encounterRound=2;
            yield return StageCombat();
            if(finished) yield break;
        }
        yield return Wait(() => SceneManager.GetActiveScene().name == "Result" && MatchResultCache.members.Count == 6,
            "waiting for authoritative match settlement", 100);
        if (finished) yield break;
        var result = MatchResultCache.members.FirstOrDefault(m => m.playerId == net.MyPlayerId);
        if (result == null || MatchResultCache.matchType != 1 || (account == "qwer" && (!result.isMvp || result.kills < 3)))
        { Finish("FAILED: ranked MVP settlement assertion"); yield break; }
        File.WriteAllText(Path.Combine(root, account + ".result"),
            $"ranked={MatchResultCache.matchType} score={MatchResultCache.scoreA}:{MatchResultCache.scoreB} MVP={result.isMvp} KDA={result.kills}/{result.deaths}/{result.assists} rank={result.rankPointsBefore}->{result.rankPointsAfter} delta={result.rankPointDelta}");
        if(account=="qwer" && Environment.GetEnvironmentVariable("VECTOR_CAPTURE_RESULT_RECONNECT")=="1")
        {
            int prior=net.CompletedReconnects; int rank=result.rankPointsAfter; int kills=result.kills;
            Field<NetworkClient>(net,"client").Close();
            yield return Wait(()=>net.ReconnectBusy,"result screen detects transport loss",10);
            yield return Wait(()=>net.CompletedReconnects>prior && !net.ReconnectBusy && SceneManager.GetActiveScene().name=="Result",
                "restoring settled result without awarding again",60);
            if(finished) yield break;
            var restored=MatchResultCache.members.First(m=>m.playerId==net.MyPlayerId);
            if(restored.rankPointsAfter!=rank || restored.kills!=kills || !restored.isMvp)
            {Finish("FAILED: restored result changed");yield break;}
            File.WriteAllText(Path.Combine(root,"result-reconnect.pass"),"PASS actual Unity Result disconnect, reconnect loading, same MVP/rank/KDA");
        }
        yield return Wait(() => FindFirstObjectByType<ResultVictorySequence>() != null &&
            FindFirstObjectByType<ResultVictorySequence>().CurrentPage == ResultVictorySequence.Page.Rank, "recording MVP and rank presentation", 20);
        if (finished) yield break;
        var sequence = FindFirstObjectByType<ResultVictorySequence>();
        yield return new WaitForSecondsRealtime(7);
        sequence.next.onClick.Invoke();
        yield return new WaitForSecondsRealtime(7);
        sequence.next.onClick.Invoke();
        yield return new WaitForSecondsRealtime(7);
        Finish("PASS: ranked duos/solos, selection, combat, server MVP, rank and rewards");
    }

    IEnumerator ProbeActions()
    {
        var net=NetworkManager.Instance; var me=net.MyPlayer;
        var menu=SettingsMenu.Instance;
        if(menu==null) { Finish("FAILED: missing authored settings menu"); yield break; }
        menu.OpenSettingsMenu();
        if(!menu.IsOpen) { Finish("FAILED: settings did not open"); yield break; }
        var position=me.transform.position;
        foreach(GameplayAction action in Enum.GetValues(typeof(GameplayAction)))
            if(GameplayActionPolicy.CanAct(me,action)) { Finish("FAILED: ESC leaked "+action); yield break; }
        var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.Space));
        yield return new WaitForSecondsRealtime(1);
        UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
        if(Vector2.Distance(new Vector2(position.x,position.z),new Vector2(me.transform.position.x,me.transform.position.z))>.15f)
        { Finish("FAILED: ESC allowed movement"); yield break; }
        menu.CloseSettingsMenu(); yield return new WaitForSecondsRealtime(.3f);
        if(Environment.GetEnvironmentVariable("VECTOR_CAPTURE_RESULT_RECONNECT")=="1")
        {
            var safe=me.transform.position; int corrections=net.MovementCorrections;
            PlaceForTest(safe+Vector3.right*500);
            yield return Wait(()=>net.MovementCorrections>corrections,"server corrects impossible movement",5);
            if(finished)yield break;
            if(Vector3.Distance(me.transform.position,safe)>1) {Finish("FAILED: correction did not restore actor");yield break;}
            File.WriteAllText(Path.Combine(root,account+".correction-pass"),"PASS server correction restores position without respawn");
        }
        var drone=me.GetComponent<InitiatorSkill_Q>();
        if(drone!=null)
        {
            yield return Wait(()=>SkillInventoryState.CanUse(SkillSlot.Q),"drone purchase confirmed",4);
            if(finished) yield break;
            Call(drone,"UseSkill");
            Call(drone,"RequestDroneDeploy");
            yield return Wait(()=>InitiatorSkill_Q.IsLocalDroneMode,"server deployed drone",5);
            if(finished) yield break;
            if(GameplayActionPolicy.CanAct(me,GameplayAction.Reload) || GameplayActionPolicy.CanAct(me,GameplayAction.WeaponChange))
            { Finish("FAILED: drone leaked weapon action"); yield break; }
            yield return new WaitForSecondsRealtime(2);
            Call(drone,"UseSkill"); yield return new WaitForSecondsRealtime(1);
            if(InitiatorSkill_Q.IsLocalDroneMode || !me.GetComponent<PlayerCameraController>().PlayerCamera.enabled ||
                !GameplayActionPolicy.CanAct(me,GameplayAction.Fire))
            { Finish("FAILED: drone exit did not restore camera/weapon"); yield break; }
        }
        File.WriteAllText(Path.Combine(root,account+".policy-probe"),"PASS ESC blocks held W/jump and seven actions"+(drone!=null?"; server drone deploy/exit, weapon lock, camera restore":""));
        yield return Wait(()=>All(".policy-probe"),"six clients action-policy probes",15);
    }

    void PlaceForTest(Vector3 position)
    {
        var me=NetworkManager.Instance.MyPlayer; var cc=me.GetComponent<CharacterController>();
        cc.enabled=false; me.transform.position=position; cc.enabled=true;
        NetworkManager.Instance.SendMoveState(position.x,position.y,position.z,me.transform.eulerAngles.y,0,0,false);
    }

    IEnumerator ProbeObjective()
    {
        var net=NetworkManager.Instance; var me=net.MyPlayer; var ps=me.GetComponent<PlayerSpike>();
        var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        try
        {
            var site=FindObjectsByType<SpikePlantZone>(FindObjectsSortMode.None).OrderBy(z=>z.name).First();
            var box=site.GetComponent<BoxCollider>(); var center=box.transform.TransformPoint(box.center);
            bool planter=me.Team==0 && me.SlotIndex==0;
            bool defender=me.Team==1 && me.SlotIndex<2;
            if(planter)
            {
                PlaceForTest(FindFirstObjectByType<MapInfo>().spikeSpawnPoint.position);
                yield return new WaitForSecondsRealtime(.5f); net.SendSpikePickupRequest();
                yield return Wait(()=>ps.hasSpike,"server granted spike carrier",5); if(finished)yield break;
                // An invalid client request must not be trusted by the server.
                net.SendSpikePlantStart(); yield return new WaitForSecondsRealtime(3.2f);
                net.SendSpikePlantComplete(me.transform.position); yield return new WaitForSecondsRealtime(.5f);
                if(RoundManager.Instance.currentState!=RoundManager.RoundState.MainRound || !ps.hasSpike)
                {Finish("FAILED: server allowed outside-site plant");yield break;}
                PlaceForTest(center); yield return new WaitForSecondsRealtime(.6f);
                if(!SpikePlantZone.Contains(me.transform.position)) {Finish("FAILED: authored plant site/root mismatch");yield break;}
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));
                yield return null; ps.TryStartPlant(); yield return new WaitForSecondsRealtime(.7f);
                SettingsMenu.Instance.OpenSettingsMenu(); yield return new WaitForSecondsRealtime(.3f);
                if(ps.IsInteracting) {Finish("FAILED: ESC did not cancel planting");yield break;}
                SettingsMenu.Instance.CloseSettingsMenu(); yield return new WaitForSecondsRealtime(.3f);
                ps.TryStartPlant();
            }
            yield return Wait(()=>Spike.PlantedSpike!=null && RoundManager.Instance.currentState==RoundManager.RoundState.SpikePlanted,
                "all clients receive server plant",25); if(finished)yield break;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            var spike=Spike.PlantedSpike;
            if(planter) PlaceForTest(center+Vector3.forward*4);
            if(defender) PlaceForTest(spike.transform.position+Vector3.right*(me.SlotIndex==0?-.6f:.6f));
            yield return new WaitForSecondsRealtime(.6f);
            File.WriteAllText(Path.Combine(root,account+".defuse-ready"),"ready");
            yield return Wait(()=>All(".defuse-ready"),"six clients ready for simultaneous defuse",8); if(finished)yield break;
            if(account=="qwer") File.WriteAllText(Path.Combine(root,"defuse.utc"),DateTimeOffset.UtcNow.AddSeconds(2).ToUnixTimeMilliseconds().ToString());
            yield return Wait(()=>File.Exists(Path.Combine(root,"defuse.utc")),"synchronize defuse requests",5); if(finished)yield break;
            yield return new WaitForSecondsRealtime(.2f);
            long utc=long.Parse(File.ReadAllText(Path.Combine(root,"defuse.utc")));
            while(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()<utc) yield return null;
            if(defender)
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));
                yield return null; spike.TryStartDefuse();
            }
            yield return new WaitForSecondsRealtime(.7f);
            bool owns=Field<bool>(spike,"isDefusing");
            File.WriteAllText(Path.Combine(root,account+".defuse-owner"),owns?"1":"0");
            yield return Wait(()=>All(".defuse-owner"),"verify single defuse owner",2); if(finished)yield break;
            if(Accounts.Count(a=>File.ReadAllText(Path.Combine(root,a+".defuse-owner"))=="1")!=1)
            {Finish("FAILED: simultaneous defuse owner count");yield break;}
            if(owns) PlaceForTest(center+Vector3.right*6);
            yield return new WaitForSecondsRealtime(1);
            if(Field<bool>(spike,"isDefusing")) {Finish("FAILED: out-of-range defuse continued");yield break;}
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return new WaitForSecondsRealtime(.5f);
            if(me.Team==1 && me.SlotIndex==0)
            {
                PlaceForTest(spike.transform.position+Vector3.right*.5f); yield return new WaitForSecondsRealtime(.6f);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));
                yield return null; spike.TryStartDefuse();
            }
            yield return Wait(()=>RoundManager.Instance.currentState==RoundManager.RoundState.RoundEnd,"server confirms defuse round result",12);
            if(finished)yield break;
            File.WriteAllText(Path.Combine(root,account+".objective-pass"),"PASS plant/ESC/exclusive defuse/range cancel/result");
            yield return Wait(()=>All(".objective-pass"),"all six confirmed objective result",3);
            yield return new WaitForSecondsRealtime(2);
        }
        finally { UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard); }
    }

    IEnumerator ReconnectEncounter()
    {
        var net=NetworkManager.Instance;
        yield return Wait(()=>All(".playing"),"six clients ready for reconnect test");
        yield return new WaitForSecondsRealtime(4);
        if(account=="qwer")
        {
            uint id=net.MyPlayerId;
            var hp=net.MyPlayer.GetComponent<PlayerHP>(); int health=hp.CurrentHP;
            var position=net.MyPlayer.transform.position;
            Stage("disconnecting TCP transport for six seconds");
            Field<NetworkClient>(net,"client").Close();
            yield return Wait(()=>net.ReconnectBusy,"connection loss detected",10);
            yield return Wait(()=>net.CompletedReconnects==1 && !net.ReconnectBusy,"restoring same player through loading screen",55);
            if(finished) yield break;
            if(net.MyPlayerId!=id || net.MyPlayer==null || net.MyPlayer.GetComponent<PlayerHP>().CurrentHP!=health ||
                Vector3.Distance(net.MyPlayer.transform.position,position)>2 ||
                FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).Length!=6 ||
                RoundManager.Instance.currentState!=RoundManager.RoundState.MainRound)
            {Finish("FAILED: reconnect identity/health/pose/roster/phase mismatch");yield break;}
            File.WriteAllText(Path.Combine(root,"reconnect.pass"),"PASS: same player ID, health, pose, six actors, main round; connection unavailable for six seconds");
        }
        else
        {
            yield return Wait(()=>File.Exists(Path.Combine(root,"reconnect.pass")),"match continues while qwer reconnects",65);
            if(finished) yield break;
            if(FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).Length!=6 || net.ReconnectBusy)
            {Finish("FAILED: other player lost during peer reconnect");yield break;}
        }
        yield return new WaitForSecondsRealtime(5);
        Stage("reconnect verified; continuing ranked combat");
    }

    [Serializable] sealed class CombatPlan { public uint hero; public byte team; public Vector3[] positions; }

    // Scripted QA encounter in the isolated one-round server. Only actor placement
    // and input are driven here: normal bullets, damage, kills and settlement run unchanged.
    IEnumerator StageCombat()
    {
        var net = NetworkManager.Instance;
        var me = net.MyPlayer;
        var planPath = Path.Combine(root, "combat-"+encounterRound+".json");
        yield return new WaitForSecondsRealtime(2);
        if (account == "qwer")
        {
            var origin = me.transform.position;
            var candidates = new System.Collections.Generic.List<Vector3>();
            for (int angle = 0; angle < 360 && candidates.Count < 3; angle += 30)
            {
                var direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                var point = origin + direction * 6;
                var ground = Physics.RaycastAll(point + Vector3.up * 4, Vector3.down, 8, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => h.collider.GetComponentInParent<PlayerIdentity>() == null && h.normal.y > .65f)
                    .OrderBy(h => h.distance).ToArray();
                if (ground.Length == 0 || Mathf.Abs(ground[0].point.y - origin.y) > 2) continue;
                point.y = ground[0].point.y + .1f;
                var from = origin + Vector3.up * 1.2f;
                var to = point + Vector3.up * 1.2f;
                if (Physics.SphereCastAll(from, .35f, (to - from).normalized, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore)
                    .Any(h => h.collider.GetComponentInParent<PlayerIdentity>() == null)) continue;
                candidates.Add(point);
            }
            if (candidates.Count < 3) { Finish("FAILED: no safe clear combat lane"); yield break; }
            File.WriteAllText(planPath, JsonUtility.ToJson(new CombatPlan { hero = me.PlayerId, team = me.Team, positions = candidates.ToArray() }));
        }
        yield return Wait(() => File.Exists(planPath), "preparing scripted combat positions", 15);
        if (finished) yield break;
        yield return new WaitForSecondsRealtime(.5f);
        var plan = JsonUtility.FromJson<CombatPlan>(File.ReadAllText(planPath));
        var movement = me.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;
        var cameraController = me.GetComponent<PlayerCameraController>();
        if (cameraController.CameraLook != null) cameraController.CameraLook.enabled = false;
        if (me.Team != plan.team)
        {
            var controller = me.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            var destination=plan.positions[me.SlotIndex % plan.positions.Length];
            // QA positioning still obeys the server's movement budget. This is not
            // a navigation/collision test; only the actual combat lane is physics checked.
            while(Vector3.Distance(me.transform.position,destination)>.01f)
            {
                me.transform.position=Vector3.MoveTowards(me.transform.position,destination,8*Time.unscaledDeltaTime);
                var step=me.transform.position;
                net.SendMoveState(step.x,step.y,step.z,me.transform.eulerAngles.y,0,0,false);
                yield return null;
            }
            if (controller != null) controller.enabled = true;
            var hero = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).First(p => p.PlayerId == plan.hero);
            me.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(hero.transform.position - me.transform.position, Vector3.up));
            var p = me.transform.position;
            net.SendMoveState(p.x, p.y, p.z, me.transform.eulerAngles.y, 0, 0, false);
        }
        File.WriteAllText(Path.Combine(root, account + ".combat-ready-"+encounterRound), "ready");
        yield return Wait(() => All(".combat-ready-"+encounterRound), "waiting for combat actors", 35);
        if (finished || account != "qwer") yield break;
        yield return new WaitForSecondsRealtime(2);
        var item = me.GetComponent<PlayerItem>();
        var camera = cameraController.PlayerCamera;
        var enemies = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).Where(p => p.Team != me.Team).OrderBy(p => p.SlotIndex).ToArray();
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            var hp = enemy.GetComponent<PlayerHP>();
            float deadline = Time.realtimeSinceStartup + 25;
            float nextShot = 0;
            Stage("actual weapon fire at " + enemy.Nickname);
            while (hp != null && !hp.IsDead && SceneManager.GetActiveScene().name != "Result")
            {
                if (Time.realtimeSinceStartup > deadline) { Finish("FAILED: weapon fire did not defeat target"); yield break; }
                var aim = enemy.transform.position + Vector3.up * 1.1f;
                var direction = aim - camera.transform.position;
                me.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(direction, Vector3.up));
                camera.transform.rotation = Quaternion.LookRotation(direction);
                var p = me.transform.position;
                net.SendMoveState(p.x, p.y, p.z, me.transform.eulerAngles.y, camera.transform.eulerAngles.x, 0, false);
                var weapon = item.GetCurrentWeapon();
                if (weapon != null && !item.IsReloading && Time.realtimeSinceStartup > nextShot)
                {
                    if (Field<int>(weapon, "currentAmmo") <= 0) Call(item, "TryStartReload");
                    else weapon.OnUse();
                    nextShot = Time.realtimeSinceStartup + .45f;
                }
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.5f);
        }
    }

    IEnumerator Capture()
    {
        var end = new WaitForEndOfFrame();
        // ScreenCapture already contains display-encoded pixels. An sRGB render target
        // would encode them again during the resize blit, washing out the recording.
        var target = new RenderTexture(960, 540, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
        while (recording)
        {
            yield return end;
            if (!recording) break;
            if(DischargeEvidence || SentinelEvidence || EntryEvidence){
                if(!dischargeCaptureReady){yield return null;continue;}
                CaptureDischargeFrame(image);
                // A 0.28 s dash needs every rendered frame, not alternate frames.
                if (!EntryDashOnly) yield return new WaitForSecondsRealtime(.025f);
                continue;
            }
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            if (frame == 0 && account == "qwer")
                File.WriteAllBytes(Path.Combine(root, "capture-source.png"), shot.EncodeToPNG());
            var previous = RenderTexture.active;
            Graphics.Blit(shot, target); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
            RenderTexture.active = previous; Destroy(shot);
            File.WriteAllBytes(Path.Combine(frames, frame.ToString("D6") + ".jpg"), image.EncodeToJPG(85));
            timeline.WriteLine(frame + "," + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); timeline.Flush(); frame++;
            yield return new WaitForSecondsRealtime(DroneEvidence?.05f:.125f);
        }
        Destroy(image); target.Release(); Destroy(target);
    }
    void Finish(string result)
    {
        if (finished) return;
        finished = true; recording = false; timeline?.Dispose(); Stage(result);
        File.WriteAllText(Path.Combine(root, account + ".done"), result);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(result.StartsWith("PASS") ? 0 : 2);
#endif
    }
}
#endif
