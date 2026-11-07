#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Game.Pings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed partial class RankedCaptureTest
{
    bool PingEvidence=>Environment.GetEnvironmentVariable("VECTOR_CAPTURE_PING")=="1";
    IEnumerator PingInput(Keyboard keyboard,Mouse mouse,int kind)
    {
        var system=FindFirstObjectByType<PingSystem>();
        // Background test windows model the active client's input focus without OS automation.
        Call(system,"OnApplicationFocus",true);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        keyboard.MakeCurrent();mouse.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(.15f);
        var camera=system.GetComponent<PingCameraProvider>().Current;
        var ray=camera.ViewportPointToRay(new Vector3(.5f,.5f,0));
        var hits=Physics.RaycastAll(ray,150,Field<LayerMask>(system,"hitMask"),QueryTriggerInteraction.Ignore);
        Debug.Log($"[PING INPUT] screen={Screen.width}x{Screen.height} viewport={camera.pixelRect} ray={ray} hits={hits.Length} focus={Field<bool>(system,"hasFocus")} gate={Field<bool>(system.GetComponent<PingBridge>(),"canRequest")}");
        foreach(var hit in hits.Take(4))Debug.Log($"[PING HIT] {hit.collider.name} layer={hit.collider.gameObject.layer} position={hit.point} actor={hit.collider.GetComponentInParent<PlayerIdentity>()}");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Z));
        if(kind==0)yield return new WaitForSecondsRealtime(.1f);
        else
        {
            yield return Wait(()=>system.IsWheelOpen,"ping wheel opens",3);if(finished)yield break;
            var item=NetworkManager.Instance.MyPlayer.GetComponent<PlayerItem>();
            var weapon=item.GetCurrentWeapon();int ammo=weapon.CurrentAmmo;int slot=item.CurrentIndex;
            weapon.OnUse();Call(item,"TryStartReload");item.TrySelectWeapon(slot==2?1:2);
            if(!DeviceCheck(weapon.CurrentAmmo==ammo&&!item.IsReloading&&item.CurrentIndex==slot,"weapon input escaped ping wheel"))yield break;
            Vector2 delta=kind==1?Vector2.right:kind==2?Vector2.down:Vector2.left;
            InputSystem.QueueDeltaStateEvent(mouse.delta,delta*120);
            yield return new WaitForSecondsRealtime(1.3f);
            if(!DeviceCheck(FindFirstObjectByType<PingManager>().wheel.SelectedIndex==kind,"wheel selection differs from actual mouse input"))yield break;
        }
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(.2f);
    }
    IEnumerator CapturePingEncounter()
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;var manager=FindFirstObjectByType<PingManager>();
        var system=FindFirstObjectByType<PingSystem>();
        if(!DeviceCheck(manager&&system&&manager.NetworkMode&&manager.hudMap&&manager.fullMap&&manager.mapCamera,"authored network ping/map references missing"))yield break;
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            dischargeCaptureReady=true;
            var target=DeviceGround(me.transform.position+me.transform.forward*8);
            AimActor(target);Call(system,"OnApplicationFocus",true);Cursor.lockState=CursorLockMode.Locked;
            yield return DeviceReady(".ping-ready");if(finished)yield break;
            for(int team=0;team<2;team++)for(int kind=0;kind<4;kind++)
            {
                int before=manager.ConfirmedCount;bool sender=me.Team==team&&me.SlotIndex==0;
                string key=$"ping-{team}-{kind}";
                yield return DeviceReady("."+key+"-ready");if(finished)yield break;
                if(sender)
                {
                    AimActor(target);DeviceEvent(key+"-input");yield return PingInput(keyboard,mouse,kind);if(finished)yield break;
                    File.WriteAllText(Path.Combine(root,key+"-sent"),"input");
                }
                yield return Wait(()=>File.Exists(Path.Combine(root,key+"-sent")),key+" sent",6);if(finished)yield break;
                yield return new WaitForSecondsRealtime(.7f);
                int expected=me.Team==team?1:0;
                if(!DeviceCheck(manager.ConfirmedCount-before==expected,"team delivery count "+key+" actual="+(manager.ConfirmedCount-before)))yield break;
                if(expected==1&&!DeviceCheck(manager.LastType==(PingType)kind&&manager.ActiveMarkerCount>0,"confirmed kind/visible marker "+key))yield break;
                DeviceEvent(key+"-verified");
                yield return DeviceReady("."+key+"-done");if(finished)yield break;
            }
            // Menu opening owns the cursor, closes the wheel and blocks new requests.
            int count=manager.ConfirmedCount;SettingsMenu.Instance.OpenSettingsMenu();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Z));yield return new WaitForSecondsRealtime(.4f);
            if(!DeviceCheck(!system.IsWheelOpen&&Cursor.lockState==CursorLockMode.None&&manager.ConfirmedCount==count,"ESC/ping cursor or input conflict"))yield break;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());SettingsMenu.Instance.CloseSettingsMenu();
            yield return new WaitForSecondsRealtime(3.2f);
            if(!DeviceCheck(manager.ActiveMarkerCount==0,"expired ping retained"))yield break;
            bool rejoin=me.Team==0&&me.SlotIndex==2;
            if(rejoin)
            {
                int reconnects=net.CompletedReconnects;Field<NetworkClient>(net,"client").Close();
                yield return Wait(()=>net.CompletedReconnects>reconnects&&!net.ReconnectBusy,"ping peer reconnect",30);if(finished)yield break;
                // Reconnect loads the game scene again; inspect the new authored HUD.
                manager=FindFirstObjectByType<PingManager>();system=FindFirstObjectByType<PingSystem>();
                if(!DeviceCheck(manager&&system,"reconnected scene has no ping HUD"))yield break;
                if(!DeviceCheck(manager.ActiveMarkerCount==0,"old ping replayed after reconnect"))yield break;
                me=net.MyPlayer;DeviceEvent("ping-reconnected");
            }
            yield return DeviceReady(".ping-rejoined",35);if(finished)yield break;
            count=manager.ConfirmedCount;
            if(me.Team==0&&me.SlotIndex==0)
            {
                AimActor(target);yield return PingInput(keyboard,mouse,0);if(finished)yield break;
                File.WriteAllText(Path.Combine(root,"ping-after-reconnect"),"sent");
            }
            yield return Wait(()=>File.Exists(Path.Combine(root,"ping-after-reconnect")),"new ping after reconnect",5);if(finished)yield break;
            yield return new WaitForSecondsRealtime(1);
            if(!DeviceCheck(manager.ConfirmedCount-count==(me.Team==0?1:0),"reconnect missed/duplicated subscription"))yield break;
            yield return DeviceReady(".ping-all-verified");if(finished)yield break;
            DeviceEvent("ping-live-pass");yield return new WaitForSecondsRealtime(3);
        }
        finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);}
    }
}
#endif
