#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed partial class RankedCaptureTest
{
    bool ReportEvidence => Environment.GetEnvironmentVariable("VECTOR_CAPTURE_REPORT") == "1";
    [Serializable] sealed class ReportPlan { public uint victim, attacker; public byte team; public Vector3 origin, position, allyOne, allyTwo; }
    IEnumerator CaptureNativeReport()
    {
        var end=new WaitForEndOfFrame();
        while(recording)
        {
            yield return end;
            if(!recording)yield break;
            if(!dischargeCaptureReady)continue;
            // Read the finished game frame, including normal post-processing,
            // viewmodels and overlay canvases. No extra camera or UI composition.
            var shot=ScreenCapture.CaptureScreenshotAsTexture();
            if(shot.width!=1920 || shot.height!=1080){Destroy(shot);Finish("FAILED: native capture is not 1920x1080");yield break;}
            if(frame==0)File.WriteAllText(Path.Combine(root,"capture-info.txt"),$"Native final game frame; {shot.width}x{shot.height}; lossless PNG; quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}; colorSpace={QualitySettings.activeColorSpace}; no resize, camera override or compositing.\n");
            File.WriteAllBytes(Path.Combine(frames,frame.ToString("D6")+".png"),shot.EncodeToPNG());Destroy(shot);
            timeline.WriteLine(frame+","+DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());timeline.Flush();frame++;
            yield return new WaitForSecondsRealtime(.033f);
        }
    }
    bool ReportAllyPosition(Vector3 origin,Vector3 forward,int sideSign,Vector3 other,out Vector3 point)
    {
        var side=Vector3.Cross(Vector3.up,forward);
        for(float back=2;back<=7;back+=1)
        for(float offset=1;offset<=3;offset+=.5f)
        {
            var candidate=DeviceGround(origin-forward*back+side*sideSign*offset);
            if(Mathf.Abs(candidate.y-origin.y)>.5f || Vector3.Distance(candidate,other)<1.2f)continue;
            if(Physics.OverlapCapsule(candidate+Vector3.up*.4f,candidate+Vector3.up*1.5f,.32f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.GetComponentInParent<PlayerIdentity>()))continue;
            var ray=origin+Vector3.up*1.3f-(candidate+Vector3.up*1.3f);
            if(Physics.RaycastAll(candidate+Vector3.up*1.3f,ray.normalized,ray.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.GetComponentInParent<PlayerIdentity>()))continue;
            point=candidate;return true;
        }
        point=default;return false;
    }
    void ReportEvent(string name)
    {
        File.AppendAllText(Path.Combine(root,account+"-report-events.csv"),DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+","+name+"\n");
        Stage(name);
    }
    IEnumerator ReportKey(Keyboard keyboard, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        yield return new WaitForSecondsRealtime(.2f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(.2f);
    }
    IEnumerator ReportClick(Mouse mouse)
    {
        InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(Screen.width*.5f,Screen.height*.5f)}.WithButton(MouseButton.Left));
        yield return new WaitForSecondsRealtime(.15f);
        InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(Screen.width*.5f,Screen.height*.5f)});
        yield return new WaitForSecondsRealtime(.25f);
    }
    IEnumerator ReportFireAt(PlayerIdentity target, bool oneShot=false)
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;var item=me.GetComponent<PlayerItem>();
        var targetHealth=target.GetComponent<PlayerHP>();int initialHealth=targetHealth.CurrentHP+targetHealth.CurrentBarrier;
        float deadline=Time.realtimeSinceStartup+35;
        while(target && !target.GetComponent<PlayerHP>().IsDead)
        {
            if(oneShot && targetHealth.CurrentHP+targetHealth.CurrentBarrier<initialHealth)yield break;
            if(!DeviceCheck(Time.realtimeSinceStartup<deadline,"live firearm did not hit report victim"))yield break;
            var weapon=item.GetCurrentWeapon();
            if(item.IsReloading){yield return null;continue;}
            if(weapon.CurrentAmmo==0){Call(item,"TryStartReload");yield return null;continue;}
            var cam=me.GetComponent<PlayerCameraController>().PlayerCamera;
            Physics.SyncTransforms();
            var candidates=target.GetComponentsInChildren<HitCollider>()
                .OrderBy(h=>h.Part==HitPart.Head?0:1).Select(h=>h.GetComponent<Collider>())
                .Where(c=>c && c.enabled && !c.isTrigger).ToArray();
            var exposed=candidates.FirstOrDefault(c=>WeaponShotTrace.FirstHit(
                new Ray(cam.transform.position,c.bounds.center-cam.transform.position),30,~0,me,me.transform,out var h)
                && h.collider.GetComponentInParent<PlayerIdentity>()==target);
            var point=exposed?exposed.bounds.center:target.transform.position+Vector3.up*1.1f;
            // Recalculate after yaw changes the offset camera's position.
            AimActor(point);AimActor(point);
            WeaponShotTrace.FirstHit(new Ray(cam.transform.position,cam.transform.forward),30,~0,me,me.transform,out var probe);
            File.AppendAllText(Path.Combine(root,account+"-aim.txt"),$"hp={targetHealth.CurrentHP} point={point} camera={cam.transform.position} error={Vector3.Angle(cam.transform.forward,point-cam.transform.position)} hit={probe.collider?.name} target={target.transform.position} candidates={string.Join(";",candidates.Select(c=>$"{c.name}:{c.bounds.center}"))}\n");
            weapon.OnUse();yield return new WaitForSecondsRealtime(.4f);
        }
    }
    string ReportSignature(ReportUI report)=>string.Join(";",report.Summaries.Select(s=>$"{s.opponentId}:{s.damageDealt}:{s.damageTaken}:{s.hitsTakenHead}:{s.hitsTakenBody}:{s.hitsTakenLeg}"));
    IEnumerator ReportAllyReload()
    {
        var me=NetworkManager.Instance.MyPlayer;string command=Path.Combine(root,"report-reload-"+me.PlayerId);
        while(!finished && !File.Exists(command))yield return null;
        if(finished)yield break;
        var plan=JsonUtility.FromJson<ReportPlan>(File.ReadAllText(Path.Combine(root,"report-plan.json")));
        AimActor(plan.position+Vector3.up*1.1f);
        var item=me.GetComponent<PlayerItem>();item.GetCurrentWeapon().OnUse();
        yield return new WaitForSecondsRealtime(.4f);Call(item,"TryStartReload");
        File.WriteAllText(command+"-started","sent");
    }
    bool ReportLayout(ReportUI report)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var t in report.GetComponentsInChildren<TMP_Text>(true))
            if(t.gameObject.activeInHierarchy && t.enabled && !string.IsNullOrWhiteSpace(t.text))
            {t.ForceMeshUpdate();if(!DeviceCheck(!t.isTextOverflowing,"report text overflow: "+t.name))return false;}
        var strip=FindFirstObjectByType<SpectatorHUD>();var hud=InGameUIManager.Instance;
        if(strip && strip.root.activeInHierarchy && hud)
        {
            Rect Bounds(Transform t){var corners=new Vector3[4];((RectTransform)t).GetWorldCorners(corners);return Rect.MinMaxRect(corners.Min(p=>p.x),corners.Min(p=>p.y),corners.Max(p=>p.x),corners.Max(p=>p.y));}
            var card=Bounds(strip.root.transform);
            if(!DeviceCheck(!card.Overlaps(Bounds(Field<UnityEngine.UI.Image>(hud,"cSkillImage").transform.parent)),"spectator card overlaps skill HUD"))return false;
            if(!DeviceCheck(!card.Overlaps(Bounds(hud.GetComponent<CombatHudReadout>().reloadRoot.transform)),"spectator card overlaps reload notice"))return false;
        }
        return true;
    }
    bool ReportViewmodels()
    {
        var net=NetworkManager.Instance;var spec=SpectatorManager.Instance;
        bool spectating=spec && spec.IsSpectating;
        foreach(var player in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
        {
            var movement=player.GetComponent<PlayerMovement>();if(!movement)continue;
            var arms=Field<GameObject>(movement,"firstPersonArmModel");if(!arms)continue;
            bool expected=!player.GetComponent<PlayerHP>().IsDead &&
                (spectating?player.PlayerId==spec.CurrentTargetPlayerId:player.PlayerId==net.MyPlayerId);
            if(!DeviceCheck(arms.activeInHierarchy==expected,"wrong first-person arms visibility: "+player.PlayerId))return false;
            if(expected)
            {
                var weapon=player.GetComponent<PlayerItem>().GetCurrentWeapon();
                // A UI/preview camera can also carry MainCamera. Check the
                // player camera that actually follows the spectator target.
                var cam=spectating?Field<Camera>(spec,"myCamera"):net.MyPlayer.GetComponent<PlayerCameraController>().PlayerCamera;
                bool visible=weapon && weapon.GetComponentsInChildren<Renderer>().Any(r=>{
                    var p=cam.WorldToViewportPoint(r.bounds.center);
                    return r.enabled && r.gameObject.activeInHierarchy && p.z>0 && p.z<2 && p.x>0 && p.x<1 && p.y>0 && p.y<1;
                });
                File.AppendAllText(Path.Combine(root,account+"-viewmodels.txt"),$"target={player.PlayerId} spectating={spectating} camera={cam.name}:{cam.transform.position} enabled={cam.enabled} arms={arms.activeInHierarchy} visible={visible}\n"+
                    string.Join("\n",weapon.GetComponentsInChildren<Renderer>(true).Select(r=>$"{r.name} enabled={r.enabled} active={r.gameObject.activeInHierarchy} viewport={cam.WorldToViewportPoint(r.bounds.center)}"))+"\n");
                if(!DeviceCheck(visible,"held weapon missing from actual output camera"))return false;
            }
        }
        return true;
    }
    bool ReportPingLayout()
    {
        var ping=FindFirstObjectByType<Game.Pings.PingManager>();
        if(!DeviceCheck(ping && ping.hudMap && ping.inputHint,"saved minimap and ping hint binding"))return false;
        Rect ScreenBounds(RectTransform r){var c=r.GetComponentInParent<Canvas>();var corners=new Vector3[4];r.GetWorldCorners(corners);var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera,p)).ToArray();return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));}
        var hint=ScreenBounds(ping.inputHint.GetComponent<RectTransform>());var map=ScreenBounds(ping.hudMap);
        return DeviceCheck(!hint.Overlaps(map) && hint.yMax<=map.yMin-8,"ping hint must sit below minimap with clear gap");
    }
    IEnumerator CaptureReportEncounter()
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;
        // The visible capture window may lose OS focus while the user works.
        // Isolate virtual test input inside this opt-in development client only.
        var background=InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        var physicalInputs=InputSystem.devices.Where(d=>d.enabled && (d is Keyboard || d is Mouse)).ToArray();
        foreach(var d in physicalInputs)InputSystem.DisableDevice(d);
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            yield return null;
            yield return Wait(()=>GameplayActionPolicy.CanAct(me.GetComponent<PlayerItem>(),GameplayAction.WeaponChange),"normal weapon input unlocked",5);if(finished)yield break;
            dischargeCaptureReady=true;
            yield return ReportKey(keyboard,Key.Digit3);
            if(!DeviceCheck(me.GetComponent<PlayerItem>().GetCurrentWeapon().GetWeaponType()==WeaponData.WeaponType.Knife,"actual knife input failed"))yield break;
            ReportEvent("knife-safe-area");yield return new WaitForSecondsRealtime(2);
            yield return ReportKey(keyboard,Key.Digit2);
            if(!DeviceCheck(me.GetComponent<PlayerItem>().GetCurrentWeapon().GetWeaponType()!=WeaponData.WeaponType.Knife,"actual pistol input failed"))yield break;
            ReportEvent("pistol-safe-area");yield return new WaitForSecondsRealtime(1);
            if(!ReportViewmodels() || !ReportPingLayout())yield break;
            var path=Path.Combine(root,"report-plan.json");
            if(account=="qwer")
            {
                var enemy=FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).Where(p=>p.Team!=me.Team).OrderBy(p=>p.SlotIndex).First();
                Vector3 candidate=me.transform.position;bool found=false;
                for(int angle=0;angle<360;angle+=30)
                {
                    var p=DeviceGround(me.transform.position+Quaternion.Euler(0,angle,0)*Vector3.forward*6);
                    var from=me.transform.position+Vector3.up*1.2f;var delta=p+Vector3.up*1.2f-from;
                    if(Mathf.Abs(p.y-me.transform.position.y)>1 || Physics.SphereCastAll(from,.35f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.GetComponentInParent<PlayerIdentity>()))continue;
                    candidate=p;found=true;break;
                }
                if(!DeviceCheck(found,"no clear live report encounter lane"))yield break;
                var heading=(candidate-me.transform.position).normalized;
                if(!DeviceCheck(ReportAllyPosition(me.transform.position,heading,1,me.transform.position,out var allyOne) && ReportAllyPosition(me.transform.position,heading,-1,allyOne,out _),"no clear spectator ally positions"))yield break;
                ReportAllyPosition(me.transform.position,heading,-1,allyOne,out var allyTwo);
                File.WriteAllText(path,JsonUtility.ToJson(new ReportPlan{victim=me.PlayerId,attacker=enemy.PlayerId,team=me.Team,origin=me.transform.position,position=candidate,allyOne=allyOne,allyTwo=allyTwo}));
            }
            yield return Wait(()=>File.Exists(path),"report fixture plan",5);if(finished)yield break;
            yield return new WaitForSecondsRealtime(.3f);
            var plan=JsonUtility.FromJson<ReportPlan>(File.ReadAllText(path));
            bool victim=me.PlayerId==plan.victim,attacker=me.PlayerId==plan.attacker;
            var target=FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).First(p=>p.PlayerId==plan.victim);
            var shooter=FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).First(p=>p.PlayerId==plan.attacker);
            if(attacker){yield return DevicePosition(plan.position);if(finished)yield break;}
            // Keep the other two allies clear of both camera-to-target fire paths.
            // Placement is a fixture; real weapon raycasts still determine every hit.
            if(!victim && me.Team==plan.team)
            {
                int index=FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).Where(p=>p.Team==plan.team && p.PlayerId!=plan.victim).OrderBy(p=>p.PlayerId).ToList().FindIndex(p=>p.PlayerId==me.PlayerId);
                yield return DevicePosition(index==0?plan.allyOne:plan.allyTwo);if(finished)yield break;
            }
            me.GetComponent<PlayerMovement>().enabled=false;
            me.GetComponent<PlayerCameraController>().CameraLook.enabled=false;
            if(victim)AimActor(shooter.transform.position+Vector3.up*1.1f);
            if(attacker)AimActor(target.transform.position+Vector3.up*1.1f);
            if(!victim && me.Team==plan.team)AimActor(plan.position+Vector3.up*1.1f);
            yield return DeviceReady(".report-positioned");if(finished)yield break;
            if(!victim && me.Team==plan.team)StartCoroutine(ReportAllyReload());
            if(victim)
            {
                if(!DeviceCheck(me.GetComponent<PlayerHP>().CurrentBarrier>0,"armor purchase missing"))yield break;
                yield return ReportFireAt(shooter,true);
                File.WriteAllText(Path.Combine(root,"report-return-shot"),"sent");
            }
            yield return Wait(()=>File.Exists(Path.Combine(root,"report-return-shot")),"victim returns actual gunfire",8);if(finished)yield break;
            if(attacker){yield return ReportFireAt(target);if(finished)yield break;}
            yield return Wait(()=>target.GetComponent<PlayerHP>().IsDead,"server confirms victim death",45);if(finished)yield break;
            if(victim)
            {
                yield return Wait(()=>ReportUI.Instance && ReportUI.Instance.Summaries.Count>0,"server report displayed",8);if(finished)yield break;
                var report=ReportUI.Instance;var summary=report.Summaries.First(s=>s.opponentId==plan.attacker);
                if(!DeviceCheck(summary.isKiller && summary.damageTaken>=100 && summary.damageDealt>0 && summary.details.Any(e=>e.barrierDamage>0),"killer/dealt/received/barrier live report mismatch"))yield break;
                string signature=ReportSignature(report);File.WriteAllText(Path.Combine(root,"report-live-values.txt"),signature);
                ReportEvent("report-summary-confirmed");if(!ReportLayout(report))yield break;
                // A remote arm rig was just enabled. Let its Animator and the
                // spectator LateUpdate produce an actual frame before checking it.
                yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();
                if(!ReportViewmodels())yield break;
                yield return new WaitForSecondsRealtime(1);
                yield return ReportKey(keyboard,Key.E);
                if(!DeviceCheck(report.IsExpanded,"E did not expand report"))yield break;
                if(!ReportLayout(report))yield break;ReportEvent("report-detail-confirmed");
                var spec=SpectatorManager.Instance;
                if(!DeviceCheck(spec.IsSpectating && spec.TargetCount==2,"two living teammates not available"))yield break;
                uint previous=spec.CurrentTargetPlayerId;
                yield return ReportClick(mouse);
                yield return new WaitForEndOfFrame();
                if(!DeviceCheck(spec.CurrentTargetPlayerId!=previous && ReportSignature(report)==signature,"target change failed or changed own combat report"))yield break;
                var observed=net.CurrentScoreboard.members.First(m=>m.playerId==spec.CurrentTargetPlayerId);
                if(!ReportViewmodels())yield break;
                var strip=FindFirstObjectByType<SpectatorHUD>();
                if(!DeviceCheck(strip.playerName.text==observed.nickname,"spectator label is not actual target"))yield break;
                var readout=FindFirstObjectByType<CombatHudReadout>();
                if(!DeviceCheck(net.TryGetCombatSnapshot(spec.CurrentTargetPlayerId,out var snapshot) &&
                    readout.health.text==snapshot.hp.ToString() && readout.armor.text==snapshot.barrier.ToString() &&
                    readout.reserve.text==snapshot.reserveAmmo.ToString() && strip.weaponName.text==ReportUI.GetSourceDisplayName(0,snapshot.weaponId,0) &&
                    readout.chargeText.text==$"{observed.ultimatePoints} / {observed.ultimateRequired}","observed health/armor/ammo/weapon/ultimate mismatch"))yield break;
                File.WriteAllText(Path.Combine(root,"report-reload-"+spec.CurrentTargetPlayerId),"fire then reload");
                yield return Wait(()=>readout.reloadRoot.activeSelf,"observed ally reload indicator",5);if(finished)yield break;
                ReportEvent("spectator-live-reload");
                if(!ReportLayout(report))yield break;
                yield return Wait(()=>!readout.reloadRoot.activeSelf,"observed reload completed",5);if(finished)yield break;
                ReportEvent("spectator-switched-record-retained");
                foreach(string menu in new[]{"TAB","ESC","VOICE","CHAT"})
                {
                    previous=spec.CurrentTargetPlayerId;
                    if(menu=="TAB")InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
                    if(menu=="ESC")SettingsMenu.Instance.OpenSettingsMenu();
                    if(menu=="VOICE")FindObjectsByType<VectorVoicePanel>(FindObjectsSortMode.None).First(v=>v.openButton.gameObject.activeInHierarchy).Toggle();
                    if(menu=="CHAT")InGameChatUI.Instance.OpenChat();
                    yield return new WaitForSecondsRealtime(.3f);
                    if(!DeviceCheck(ReportUI.OverlayBlocked && report.presentation.alpha==0,"overlay not suppressing report: "+menu))yield break;
                    yield return ReportClick(mouse);
                    if(!DeviceCheck(spec.CurrentTargetPlayerId==previous && ReportSignature(report)==signature,"menu click changed spectator target/report: "+menu))yield break;
                    if(menu=="TAB")InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                    if(menu=="ESC")SettingsMenu.Instance.CloseSettingsMenu();
                    if(menu=="VOICE")foreach(var voice in FindObjectsByType<VectorVoicePanel>(FindObjectsSortMode.None))voice.Close();
                    if(menu=="CHAT")InGameChatUI.Instance.CloseChat();
                    yield return new WaitForSecondsRealtime(.3f);
                    if(!DeviceCheck(report.presentation.alpha==1,"report did not return after "+menu))yield break;
                    ReportEvent("menu-"+menu+"-pass");
                }
                int reconnects=net.CompletedReconnects;uint identity=net.MyPlayerId;
                Field<NetworkClient>(net,"client").Close();
                yield return Wait(()=>net.CompletedReconnects>reconnects && !net.ReconnectBusy,"dead player real reconnect",35);if(finished)yield break;
                yield return Wait(()=>ReportUI.Instance && ReportUI.Instance.Summaries.Count>0,"report recovered after reconnect",8);if(finished)yield break;
                report=ReportUI.Instance;spec=SpectatorManager.Instance;
                if(!DeviceCheck(net.MyPlayerId==identity && net.MyPlayer.GetComponent<PlayerHP>().IsDead && spec.IsSpectating && ReportSignature(report)==signature,"dead reconnect lost identity/report/spectator"))yield break;
                ReportEvent("dead-reconnect-restored");yield return new WaitForSecondsRealtime(1);
                yield return new WaitForEndOfFrame();
                if(!ReportViewmodels())yield break;
                File.WriteAllText(Path.Combine(root,"report-observer-finished"),"pass");
                SettingsMenu.Instance.OpenSettingsMenu();
            }
            else if(!DeviceCheck(!ReportUI.Instance.IsVisible,"living player saw combat report"))yield break;
            yield return Wait(()=>File.Exists(Path.Combine(root,"report-observer-finished")),"spectator menus and reconnect checked",65);if(finished)yield break;
            // Finish via normal round clock. No server timers, round rules or damage values are modified.
            yield return Wait(()=>RoundManager.Instance.currentState==RoundManager.RoundState.Preparation,"ordinary next round",110);if(finished)yield break;
            if(!DeviceCheck(!net.MyPlayer.GetComponent<PlayerHP>().IsDead && !ReportUI.Instance.IsVisible && ReportUI.Instance.Summaries.Count==0 && !SpectatorManager.Instance.IsSpectating,"next round retained death/report/spectator"))yield break;
            if(victim)
            {
                if(!DeviceCheck(SettingsMenu.Instance.IsOpen,"respawn closed settings"))yield break;
                SettingsMenu.Instance.CloseSettingsMenu();ReportEvent("respawn-cleared-report-preserved-ESC");
                yield return new WaitForEndOfFrame();
                if(!ReportViewmodels() || !ReportPingLayout())yield break;
            }
            yield return DeviceReady(".report-pass",8);if(finished)yield break;
            ReportEvent("report-live-pass");yield return new WaitForSecondsRealtime(1);
        }
        finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);foreach(var d in physicalInputs)if(d.added)InputSystem.EnableDevice(d);InputSystem.settings.backgroundBehavior=background;}
    }
}
#endif
