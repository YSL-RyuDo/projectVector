#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed partial class RankedCaptureTest
{
    bool DeviceEvidence=>Environment.GetEnvironmentVariable("VECTOR_CAPTURE_DEVICE")=="1";
    int deviceRemotePlants,deviceRemoteDefuses,deviceRecoveries;
    void LateUpdate()
    {
        if(!DeviceEvidence||!dischargeCaptureReady)return;
        foreach(var p in FindObjectsByType<SpikeInteractionPresentation>(FindObjectsSortMode.None))
        {
            if(p.IsRecovering)deviceRecoveries++;
            if(p.GetComponent<PlayerIdentity>()?.PlayerId==NetworkManager.Instance.MyPlayerId)continue;
            if(p.IsPresenting&&p.thirdPerson.device.activeSelf)deviceRemotePlants++;
            if(p.IsPresenting&&p.thirdPerson.tool.activeSelf)deviceRemoteDefuses++;
        }
    }
    void DeviceEvent(string name)
    {
        File.AppendAllText(Path.Combine(root,account+"-device-events.csv"),DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+","+name+"\n");Stage(name);
    }
    bool DeviceCheck(bool ok,string why){if(!ok)Finish("FAILED: "+why);return ok;}
    Vector3 DeviceGround(Vector3 point)
    {
        var hits=Physics.RaycastAll(point+Vector3.up*2,Vector3.down,8,~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.collider.GetComponentInParent<PlayerIdentity>()==null&&h.normal.y>.7f).OrderBy(h=>h.distance).ToArray();
        return hits.Length>0?hits[0].point+Vector3.up*.05f:point;
    }
    IEnumerator DevicePosition(Vector3 destination)
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;var movement=me.GetComponent<PlayerMovement>();
        var cc=me.GetComponent<CharacterController>();var look=me.GetComponent<PlayerCameraController>().CameraLook;
        movement.enabled=false;look.enabled=false;cc.enabled=false;
        // Fixture placement only, within the server movement budget. Navigation
        // and collision traversal are not under test; live F/fire input follows.
        float deadline=Time.realtimeSinceStartup+40;
        while(Vector3.Distance(me.transform.position,destination)>.03f&&Time.realtimeSinceStartup<deadline)
        {
            me.transform.position=Vector3.MoveTowards(me.transform.position,destination,5*Time.unscaledDeltaTime);
            var p=me.transform.position;net.SendMoveState(p.x,p.y,p.z,me.transform.eulerAngles.y,0,1,false);yield return null;
        }
        cc.enabled=true;movement.enabled=true;
        yield return new WaitForSecondsRealtime(.4f);
        DeviceCheck(Vector3.Distance(me.transform.position,destination)<.8f,"server rejected fixture placement");
    }
    IEnumerator DeviceReady(string suffix,float timeout=45)
    {
        File.WriteAllText(Path.Combine(root,account+suffix),"ready");
        yield return Wait(()=>All(suffix),"six peers "+suffix,timeout);
    }
    IEnumerator DeviceGunRecovered()
    {
        var me=NetworkManager.Instance.MyPlayer;var item=me.GetComponent<PlayerItem>();
        yield return new WaitForSecondsRealtime(.7f);
        var view=me.GetComponent<PlayerWeaponViewController>();
        if(!DeviceCheck(!me.GetComponent<PlayerSpike>().IsInteracting&&item.GetCurrentWeapon()!=null&&
            item.GetCurrentWeapon().gameObject.activeInHierarchy&&Field<GameObject>(view,"firstPersonWeaponHolder").activeInHierarchy&&
            !Field<GameObject>(view,"thirdPersonWeaponHolder").activeInHierarchy,"cancel left input locked or duplicate/hidden gun"))yield break;
        AimActor(me.transform.position+Vector3.up*8+me.transform.forward*5);
        int ammo=item.GetCurrentWeapon().CurrentAmmo;
        yield return ClickCaptureMouse();yield return new WaitForSecondsRealtime(.3f);
        DeviceCheck(item.GetCurrentWeapon().CurrentAmmo<ammo,"live firing did not recover after cancel");
        DeviceEvent("cancel-firearm-recovered");
    }
    IEnumerator CaptureDeviceEncounter()
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;var ps=me.GetComponent<PlayerSpike>();
        bool planter=me.Team==0&&me.SlotIndex==0,defuser=me.Team==1&&me.SlotIndex==0,rejoin=me.Team==1&&me.SlotIndex==2;
        var actors=FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        var planterActor=actors.First(p=>p.Team==0&&p.SlotIndex==0);
        var defuserActor=actors.First(p=>p.Team==1&&p.SlotIndex==0);
        var site=FindObjectsByType<SpikePlantZone>(FindObjectsSortMode.None).OrderBy(z=>z.name).First();
        var box=site.GetComponent<BoxCollider>();var center=DeviceGround(box.transform.TransformPoint(box.center));
        var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
        void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        try
        {
            dischargeCaptureReady=true;
            if(planter)
            {
                yield return DevicePosition(FindFirstObjectByType<MapInfo>().spikeSpawnPoint.position);if(finished)yield break;
                net.SendSpikePickupRequest();yield return Wait(()=>ps.hasSpike,"server carrier acquisition",5);if(finished)yield break;
            }
            var destination=planter?center:defuser?center+Vector3.forward*1.1f:center+new Vector3((me.Team==0?-1:1)*(2+me.SlotIndex),0,-2);
            yield return DevicePosition(DeviceGround(destination));if(finished)yield break;
            AimActor(center+Vector3.up*.8f);
            if(planter)AimActor(center+Vector3.forward*5+Vector3.up*1.5f);
            yield return DeviceReady(".device-positioned");if(finished)yield break;
            if(planter)
            {
                Keys(Key.F);yield return new WaitForSecondsRealtime(.8f);
                var item=me.GetComponent<PlayerItem>();var weapon=item.GetCurrentWeapon();int ammo=weapon.CurrentAmmo;
                weapon.OnUse();bool switched=item.TrySelectWeapon(2);Call(item,"TryStartReload");
                if(!DeviceCheck(ps.IsPlanting&&me.GetComponent<SpikeInteractionPresentation>().IsPresenting&&weapon.CurrentAmmo==ammo&&!switched&&!item.IsReloading,"plant input/pose/weapon exclusion"))yield break;
                DeviceEvent("plant-key-release");Keys();yield return DeviceGunRecovered();if(finished)yield break;
                AimActor(center+Vector3.forward*5+Vector3.up*1.5f);
                Keys(Key.F);yield return new WaitForSecondsRealtime(.8f);SettingsMenu.Instance.OpenSettingsMenu();
                yield return new WaitForSecondsRealtime(.4f);
                if(!DeviceCheck(!ps.IsInteracting,"ESC failed to cancel plant"))yield break;
                Keys();SettingsMenu.Instance.CloseSettingsMenu();yield return DeviceGunRecovered();if(finished)yield break;
                AimActor(center+Vector3.forward*5+Vector3.up*1.5f);
                DeviceEvent("plant-full-start");Keys(Key.F);
            }
            yield return Wait(()=>Spike.PlantedSpike&&Spike.PlantedSpike.isPlanted,"authoritative plant on six clients",25);if(finished)yield break;
            Keys();DeviceEvent("plant-confirmed");yield return new WaitForSecondsRealtime(1);
            if(!DeviceCheck(FindObjectsByType<PlayerSpike>(FindObjectsSortMode.None).All(p=>!p.hasSpike),"remote carrier not cleared"))yield break;
            if(!planter&&!DeviceCheck(deviceRemotePlants>0&&deviceRecoveries>0,"remote plant or stand-up missing"))yield break;
            AimActor(Spike.PlantedSpike.transform.position+Vector3.up*.3f);
            if(defuser)
            {
                Keys(Key.F);yield return Wait(()=>ps.IsDefusing,"defuse accepted",4);if(finished)yield break;
                DeviceEvent("defuse-half-start");
                yield return Wait(()=>Spike.PlantedSpike.DefuseProgress>.57f,"half checkpoint reached",4);if(finished)yield break;
                Keys();yield return new WaitForSecondsRealtime(.4f);
                if(!DeviceCheck(!ps.IsInteracting,"defuse release did not cancel"))yield break;
                File.WriteAllText(Path.Combine(root,"device-half-ready"),"saved");DeviceEvent("half-cancelled");
            }
            yield return Wait(()=>File.Exists(Path.Combine(root,"device-half-ready")),"wait for half cancellation",10);if(finished)yield break;
            if(rejoin)
            {
                uint id=net.MyPlayerId;float before=Spike.PlantedSpike.GetComponent<SpikePresentation>().Remaining;int count=net.CompletedReconnects;
                Field<NetworkClient>(net,"client").Close();
                yield return Wait(()=>net.CompletedReconnects>count&&!net.ReconnectBusy,"reconnect to planted device",30);if(finished)yield break;
                me=net.MyPlayer;ps=me.GetComponent<PlayerSpike>();
                if(!DeviceCheck(net.MyPlayerId==id&&Spike.PlantedSpike&&Spike.PlantedSpike.GetComponent<SpikePresentation>().Remaining<before&&
                    Mathf.Abs(Spike.PlantedSpike.DefuseProgress-.5f)<.01f&&FindObjectsByType<Spike>(FindObjectsSortMode.None).Count(s=>s.isPlanted)==1,"reconnect device/time/checkpoint mismatch"))yield break;
                File.WriteAllText(Path.Combine(root,"device-reconnect-pass"),"same device, lower remaining time, half saved");DeviceEvent("reconnect-restored");
            }
            yield return Wait(()=>File.Exists(Path.Combine(root,"device-reconnect-pass")),"observer restores checkpoint",30);if(finished)yield break;
            if(defuser){DeviceEvent("defuse-resume");Keys(Key.F);}
            yield return Wait(()=>RoundManager.Instance.currentState==RoundManager.RoundState.RoundEnd,"server defuse result",9);if(finished)yield break;
            Keys();DeviceEvent("defuse-result");yield return new WaitForSecondsRealtime(.9f);
            if(!DeviceCheck(!ps.IsInteracting&&!me.GetComponent<SpikeInteractionPresentation>().IsPresenting,"round result retained device lock"))yield break;
            if(!defuser&&!DeviceCheck(deviceRemoteDefuses>0,"remote defuse not visible"))yield break;
            yield return DeviceReady(".device-defused",4);if(finished)yield break;
            yield return Wait(()=>RoundManager.Instance.currentState==RoundManager.RoundState.Preparation,"next round cleanup",15);if(finished)yield break;
            if(!DeviceCheck(!Spike.PlantedSpike&&FindObjectsByType<SpikeInteractionPresentation>(FindObjectsSortMode.None).All(p=>!p.IsPresenting),"round reset retained objective"))yield break;
            yield return Wait(()=>RoundManager.Instance.currentState==RoundManager.RoundState.MainRound,"normal buy phase finishes",35);if(finished)yield break;
            me=net.MyPlayer;ps=me.GetComponent<PlayerSpike>();
            if(planter)
            {
                yield return DevicePosition(FindFirstObjectByType<MapInfo>().spikeSpawnPoint.position);if(finished)yield break;
                net.SendSpikePickupRequest();yield return Wait(()=>ps.hasSpike,"round two carrier",5);if(finished)yield break;
            }
            yield return DevicePosition(DeviceGround(planter?center:center+new Vector3(3+me.SlotIndex,0,-3)));if(finished)yield break;
            AimActor(center+Vector3.up*.4f);if(planter)AimActor(center+Vector3.forward*5+Vector3.up*1.5f);
            yield return DeviceReady(".device-second-ready");if(finished)yield break;
            if(planter)Keys(Key.F);
            yield return Wait(()=>Spike.PlantedSpike&&Spike.PlantedSpike.isPlanted,"round two plant",8);if(finished)yield break;
            Keys();float start=Time.realtimeSinceStartup;DeviceEvent("normal-45-second-countdown");
            if(planter){yield return DevicePosition(DeviceGround(center+Vector3.back*4));if(finished)yield break;AimActor(center+Vector3.up*.4f);}
            yield return Wait(()=>RoundManager.Instance.currentState==RoundManager.RoundState.RoundEnd,"server energy release at normal deadline",48);if(finished)yield break;
            DeviceCheck(Time.realtimeSinceStartup-start>=43,"device completed before original 45-second rule");if(finished)yield break;
            DeviceEvent("server-energy-release");yield return new WaitForSecondsRealtime(1);
            if(!DeviceCheck(FindObjectsByType<SpikeDetonation>(FindObjectsSortMode.None).Length==1,"missing/duplicate server release effect"))yield break;
            yield return DeviceReady(".device-complete",4);yield return new WaitForSecondsRealtime(1);
        }
        finally{Keys();InputSystem.RemoveDevice(keyboard);}
    }
}
#endif
