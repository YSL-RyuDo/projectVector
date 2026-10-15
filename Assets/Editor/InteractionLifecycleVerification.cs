using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InteractionLifecycleVerification
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string reason) { if (!ok) throw new Exception(reason); }
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Flags).SetValue(o, value);
    static void Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, Flags).Invoke(o, args);
    public static void Run()
    {
        Check(Application.isBatchMode, "Batch only");
        foreach (string scene in new[] { "Login", "Lobby", "Game_TestVersion", "Result", "RankResult", "Practice" })
        {
            Check(!VectorVoicePanel.ShouldShow(scene, false, true, true), "Unauthenticated shortcut");
            bool game = scene == "Game_TestVersion";
            Check(VectorVoicePanel.ShouldShow(scene, true, false, false) == game, "Outside room shortcut: " + scene);
            Check(VectorVoicePanel.ShouldShow(scene, true, true, false) == (game || scene == "Lobby"), "Temp room shortcut");
            Check(VectorVoicePanel.ShouldShow(scene, true, false, true) == (game || scene == "Lobby"), "Private room shortcut");
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var actor = new GameObject("InteractionFixture");
        var hp = actor.AddComponent<PlayerHP>(); hp.SetHp(100);
        var item = actor.AddComponent<PlayerItem>();
        var spike = actor.AddComponent<PlayerSpike>(); Set(spike, "playerItem", item);
        Set(spike, "isPlanting", true);
        Check((bool)typeof(PlayerItem).GetMethod("WeaponActionBlocked", Flags).Invoke(item, null), "Plant allows weapon switch/reload");
        spike.ForceCancelPlant(); Check(!spike.IsInteracting, "Plant cancellation retained input lock");
        var cameraRoot = new GameObject("Camera"); cameraRoot.transform.SetParent(actor.transform);
        var camera = cameraRoot.AddComponent<Camera>();
        var drone = actor.AddComponent<InitiatorSkill_Q>(); Call(drone, "Awake");
        Call(drone, "SetLocalDroneMode", true);
        Check(InitiatorSkill_Q.IsLocalDroneMode && !camera.enabled, "Drone entry");
        Call(drone, "ForceReturnToPlayerCamera"); Check(!InitiatorSkill_Q.IsLocalDroneMode && camera.enabled, "Alive drone exit");
        hp.SetHp(0); Call(drone, "SetLocalDroneMode", true);
        Check(!InitiatorSkill_Q.IsLocalDroneMode, "Dead actor accepted drone mode");
        camera.enabled = false; Call(drone, "ForceReturnToPlayerCamera");
        Check(!camera.enabled, "Dead drone exit restored player camera over spectator");
        var blade = actor.AddComponent<EntrySkill_X>(); blade.ApplyBladeStateFromServer(true, 5, 10);
        Check(!(bool)typeof(EntrySkill_X).GetField("_isBladeMode", Flags).GetValue(blade), "Late ultimate activated dead actor");
        var planted = new GameObject("DefuseFixture").AddComponent<Spike>();
        Set(planted, "isDefusing", true); Set(planted, "defuseStartRequested", true);
        planted.ForceCancelInteraction();
        Check(!(bool)typeof(Spike).GetField("isDefusing", Flags).GetValue(planted), "Defuse round cleanup");
        UnityEngine.Object.DestroyImmediate(planted.gameObject); UnityEngine.Object.DestroyImmediate(actor);
        var round = new GameObject("RoundFixture").AddComponent<RoundManager>(); Call(round, "Awake");
        foreach (string map in new[] { "TestMap", "Desert" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Maps/{map}.prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var info = root.GetComponent<MapInfo>(); Check(info != null && info.barriers != null, map + " authored barriers");
            round.SetMapInfo(info); round.OnServerPrepareStarted(); Check(round.IsBuyPhase && info.barriers.activeSelf, map + " prepare barrier");
            round.OnServerPrepareFinished();
            Check(!round.IsBuyPhase && round.CanAttack && !info.barriers.activeSelf && round.currentState == RoundManager.RoundState.MainRound, map + " prepare-end unlock");
            UnityEngine.Object.DestroyImmediate(root);
        }
        UnityEngine.Object.DestroyImmediate(round.gameObject);
        string report = "PASS voice scene/room eligibility, plant weapon lock/cancel, live/dead drone restoration, late ultimate rejection, defuse cleanup, TestMap/Desert buy-end barriers and attack state";
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "VectorInteractionLifecycle.txt"), report);
        Debug.Log("INTERACTION_LIFECYCLE_PASS: " + report);
    }
    public static void RunRegression()
    {
        Run(); MultiplayerMotionVerification.Run(); SpawnFacingVerification.Run(); RunEsc(); RunPolicy(); RunSettingsPersistence();
    }

    public static void RunSettingsPersistence()
    {
        Check(Application.isBatchMode, "Batch only");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var flow = new GameObject("SettingsFlow").AddComponent<GameController>();
        typeof(GameController).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, flow);
        var menu = new GameObject("SettingsFixture").AddComponent<SettingsMenu>(); Call(menu, "Awake");
        var panel = new GameObject("AuthoredPanelFixture"); Set(menu, "settingsPanel", panel);
        var slider = new GameObject("Sensitivity", typeof(RectTransform)).AddComponent<UnityEngine.UI.Slider>();
        slider.transform.SetParent(panel.transform); slider.value = .37f;
        var actor = new GameObject("RespawnFixture");
        var hp = actor.AddComponent<PlayerHP>(); hp.SetHp(100);
        var death = actor.AddComponent<PlayerDeathController>(); Call(death, "Awake");
        var look = new GameObject("View").AddComponent<FPSCameraLook>(); look.transform.SetParent(actor.transform);
        try
        {
            panel.SetActive(true); flow.ShowUICursor();
            death.OnDead(); hp.SetHp(100); death.OnRespawn();
            // ApplyRoundResetToPlayer reconnects the camera through SetLocalPlayer.
            look.SetLocalPlayer(true, actor.transform);
            Check(panel.activeInHierarchy && menu.IsOpen, "Respawn closed settings");
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Respawn camera stole settings cursor");
            flow.EnterPlayingPhase(); flow.LockGameplayCursor();
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Playing transition stole settings cursor");
            Check(Mathf.Abs(slider.value - .37f) < .001f, "Respawn reset the current settings edit");
            Check(GameplayActionPolicy.MenuBlocksInput, "Respawn released settings input lock");
            var smoke = actor.AddComponent<ControllerSkill_E>();
            Call(smoke, "ForceCloseMiniMapImmediate");
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Skill teardown stole settings cursor");
            var store = new GameObject("StoreFixture").AddComponent<StorePanelUI>();
            var storePanel = new GameObject("StorePanel"); Set(store, "storePanelRoot", storePanel);
            store.Close();
            Check(menu.IsOpen && Cursor.lockState == CursorLockMode.None && Cursor.visible, "Buy-end close stole settings cursor");
            UnityEngine.Object.DestroyImmediate(store.gameObject); UnityEngine.Object.DestroyImmediate(storePanel);
            panel.SetActive(false); flow.LockGameplayCursor();
            Check(!Cursor.visible && (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null ||
                Cursor.lockState == CursorLockMode.Locked), "Closed settings did not restore gameplay cursor");
            Debug.Log("SETTINGS_PERSISTENCE_PASS: death/respawn camera rebind, round flow, skill teardown, buy-end close preserve panel/cursor/edit/input; closed panel restores gameplay cursor");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(actor);
            UnityEngine.Object.DestroyImmediate(menu.gameObject); UnityEngine.Object.DestroyImmediate(panel);
            UnityEngine.Object.DestroyImmediate(flow.gameObject);
        }
    }

    public static void RunPolicy()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var actor=new GameObject("PolicyFixture"); var hp=actor.AddComponent<PlayerHP>(); hp.SetHp(100);
        var menu=new GameObject("MenuFixture").AddComponent<SettingsMenu>(); Call(menu,"Awake");
        var panel=new GameObject("MenuPanel"); Set(menu,"settingsPanel",panel); panel.SetActive(true);
        foreach(GameplayAction action in Enum.GetValues(typeof(GameplayAction)))
            Check(!GameplayActionPolicy.CanAct(hp,action),"Menu leaked "+action);
        Check(GameplayActionPolicy.CanAct(hp,GameplayAction.Reload,false),"Menu cancels already-authorized reload");
        panel.SetActive(false); hp.SetHp(0);
        foreach(GameplayAction action in Enum.GetValues(typeof(GameplayAction)))
            Check(!GameplayActionPolicy.CanAct(hp,action),"Dead actor leaked "+action);
        hp.SetHp(100);
        var net=new GameObject("RestoringNetwork").AddComponent<NetworkManager>(); NetworkManager.Instance=net;
        Set(net,"<ReconnectBusy>k__BackingField",true);
        foreach(GameplayAction action in Enum.GetValues(typeof(GameplayAction)))
            Check(!GameplayActionPolicy.CanAct(hp,action),"Reconnecting actor leaked "+action);
        UnityEngine.Object.DestroyImmediate(net.gameObject);
        UnityEngine.Object.DestroyImmediate(menu.gameObject); UnityEngine.Object.DestroyImmediate(panel); UnityEngine.Object.DestroyImmediate(actor);
        Debug.Log("ACTION_POLICY_PASS: seven action types blocked by menu/death/reconnect; accepted reload unaffected by menu");
    }

    public static void RunEsc()
    {
        Check(Application.isBatchMode, "Batch only");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var flow = new GameObject("EscFlow").AddComponent<GameController>();
        typeof(GameController).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, flow);
        var menu = new GameObject("EscSettings").AddComponent<SettingsMenu>(); Call(menu, "Awake");
        var panel = new GameObject("EscPanel"); Set(menu, "settingsPanel", panel);
        panel.SetActive(true);
        var actor = new GameObject("EscLocal").AddComponent<PlayerMovement>(); Call(actor, "Awake");
        Set(actor, "_isMine", true);
        var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        NetworkManager network = null;
        NetworkClient transport = null;
        System.Net.Sockets.TcpClient peer = null;
        try
        {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W, UnityEngine.InputSystem.Key.Space));
            UnityEngine.InputSystem.InputSystem.Update();
            Check((Vector2)typeof(PlayerMovement).GetMethod("getInput", Flags).Invoke(actor, null) == Vector2.zero,
                "ESC allowed held movement input");
            Call(actor, "Update");
            Check(actor.PlanarSpeed == 0f, "ESC retained walking speed");
            panel.SetActive(false);
            Check(((Vector2)typeof(PlayerMovement).GetMethod("getInput", Flags).Invoke(actor, null)).y == 1,
                "Closing ESC did not restore movement input");
            panel.SetActive(true);

            // Actual encoded packets through loopback TCP, not a mocked send counter.
            listener.Start();
            network = new GameObject("EscNetwork").AddComponent<NetworkManager>();
            typeof(NetworkManager).GetField("Instance", BindingFlags.Static | BindingFlags.Public).SetValue(null, network);
            transport = (NetworkClient)typeof(NetworkManager).GetField("client", Flags).GetValue(network);
            transport.Connect("127.0.0.1", ((System.Net.IPEndPoint)listener.LocalEndpoint).Port);
            peer = listener.AcceptTcpClient(); peer.GetStream().ReadTimeout = 3000;
            Set(flow, "<IsPaused>k__BackingField", true);
            actor.CanMove = false; // Input lock must not lock state replication.
            byte[] ReadMove()
            {
                var bytes = new byte[34]; int offset = 0;
                while (offset < bytes.Length)
                {
                    int count = peer.GetStream().Read(bytes, offset, bytes.Length - offset);
                    Check(count > 0, "Loopback closed"); offset += count;
                }
                Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length &&
                    BitConverter.ToUInt16(bytes, 2) == (ushort)PacketType.PKT_CS_MOVE_STATE, "Movement wire format");
                return bytes;
            }
            Set(actor, "lastSendTime", Time.unscaledTime - 1f); Call(actor, "Update"); ReadMove();
            Set(actor, "lastSendTime", Time.unscaledTime - 0.25f); Call(actor, "Update"); ReadMove();
            actor.transform.position = new Vector3(12, 3, -9);
            actor.ResetAfterRespawn(90); Call(actor, "Update");
            var spawned = ReadMove();
            Check(BitConverter.ToSingle(spawned, 4) == 12 && BitConverter.ToSingle(spawned, 12) == -9,
                "ESC blocked next-round spawn packet");
            var remote = new GameObject("EscRemote").AddComponent<PlayerMovement>(); Call(remote, "Awake");
            remote.OnReceiveRemoteMove(new Vector3(5, 0, 6), 45, 0, 0, false);
            Call(remote, "Update");
            Check(remote.transform.position == new Vector3(5, 0, 6), "ESC froze remote movement");
            UnityEngine.Object.DestroyImmediate(remote.gameObject);
            Debug.Log("ESC_NETWORK_PASS: held-input blocked, close restores input, actual TCP idle heartbeat and respawn position under ESC/pause, remote update continues");
        }
        finally
        {
            transport?.Close(); peer?.Close(); listener.Stop();
            UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
            UnityEngine.Object.DestroyImmediate(actor.gameObject);
            UnityEngine.Object.DestroyImmediate(menu.gameObject);
            UnityEngine.Object.DestroyImmediate(panel);
            UnityEngine.Object.DestroyImmediate(flow.gameObject);
            if (network != null) UnityEngine.Object.DestroyImmediate(network.gameObject);
        }
    }
}
