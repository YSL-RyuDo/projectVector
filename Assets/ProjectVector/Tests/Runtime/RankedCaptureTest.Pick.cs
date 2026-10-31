#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public sealed partial class RankedCaptureTest
{
    bool PickEvidence => Environment.GetEnvironmentVariable("VECTOR_CAPTURE_PICK") == "1";
    int exchangedCharacter;
    IEnumerator CapturePickActions()
    {
        var pick=PickManager.Instance;
        yield return Wait(()=>Field<PickStateData>(pick,"currentState").members.All(m=>m.isLocked),"all allies locked for exchange",7);if(finished)yield break;
        var team=Field<PickStateData>(pick,"currentState").members.OrderBy(m=>m.playerId).ToArray();int mine=Array.FindIndex(team,m=>m.isMe);
        var slots=Field<TeamPickSlotUI[]>(pick,"teamPickSlots");var view=Field<PickInteractionView>(pick,"interactionView");
        for(int i=0;i<3;i++) {
            var slot=slots[team[i].slotIndex];
            foreach(var name in new[]{"changeButton","shareButton"}) {
                var b=Field<Button>(slot,name);var surface=b.GetComponent<Image>();
                if(!DeviceCheck(surface.sprite==null && surface.color.r<.4f,"saved dark action surface"))yield break;
            }
            if(!DeviceCheck(Field<Button>(slot,"changeButton").gameObject.activeSelf==(i!=mine),"exchange hidden only on own row"))yield break;
            if(!DeviceCheck(Field<Button>(slot,"shareButton").gameObject.activeSelf==(i==mine),"showcase own row only"))yield break;
        }
        if(mine==0)Field<Button>(slots[team[0].slotIndex],"shareButton").onClick.Invoke();
        yield return new WaitForSecondsRealtime(1);
        if(!DeviceCheck(mine==0?view.noticeText.text=="승률 또는 플레이 수가 부족합니다":!view.noticePanel.activeSelf,"low stats local-only notice"))yield break;
        if(account=="qwer")yield return SavePickFrame("Pick-Private-Live");
        if(mine==0)Field<Button>(slots[team[1].slotIndex],"changeButton").onClick.Invoke();
        yield return Wait(()=>Field<PickActionsState>(pick,"actionState").members.Any(m=>m.exchangeId!=0),"real exchange request",5);if(finished)yield break;
        if(!DeviceCheck(view.exchangePanel.activeSelf==(mine<2),"only involved players receive exchange popup"))yield break;
        foreach(var m in team)if(m.playerId!=team[mine].playerId && m!=team[2])
            if(!DeviceCheck(!Field<Button>(slots[m.slotIndex],"changeButton").interactable,"busy partner cannot receive third-party request"))yield break;
        if(account=="qwer")yield return SavePickFrame("Pick-Exchange-Live");
        if(mine==1)yield return SavePickFrame("Pick-Incoming-"+account);
        yield return new WaitForSecondsRealtime(1);
        if(mine==1)view.acceptButton.onClick.Invoke();
        yield return Wait(()=>Field<PickActionsState>(pick,"actionState").members.All(m=>m.exchangeId==0),"real acceptance clears both locks",5);if(finished)yield break;
        var after=Field<PickStateData>(pick,"currentState").members;
        exchangedCharacter=after.First(m=>m.isMe).lockedCharacterId;
        if(!DeviceCheck(after.First(m=>m.playerId==team[0].playerId).lockedCharacterId==1 && after.First(m=>m.playerId==team[1].playerId).lockedCharacterId==0,"swap reflected in all ally portraits"))yield break;
        File.WriteAllText(Path.Combine(root,account+".pick-pass"),"PASS own-row actions, private low stats, both busy, accept and swap");
        if(account=="qwer")yield return SavePickFrame("Pick-Accepted-Live");
    }
    IEnumerator SavePickFrame(string name)
    {
        yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(root,name+".png"),image.EncodeToPNG());Destroy(image);
    }
    IEnumerator VerifyPickSpawn()
    {
        if(!DeviceCheck(NetworkManager.Instance.MyPlayer.CharacterId==exchangedCharacter,"swapped character actually spawned"))yield break;
        if(account=="qwer") {
            var background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var physical=InputSystem.devices.Where(d=>d.enabled && (d is Keyboard || d is Mouse)).ToArray();
            foreach(var d in physical)InputSystem.DisableDevice(d);
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try {
            yield return null;
            yield return Wait(()=>GameplayActionPolicy.CanAct(NetworkManager.Instance.MyPlayer.GetComponent<PlayerItem>(),GameplayAction.WeaponChange),"normal knife input unlocked",5);if(finished)yield break;
            yield return ReportKey(keyboard,UnityEngine.InputSystem.Key.Digit3);yield return new WaitForSecondsRealtime(1);
            var knife=NetworkManager.Instance.MyPlayer.GetComponent<PlayerItem>().GetCurrentWeapon();
            if(!DeviceCheck(knife.GetWeaponType()==WeaponData.WeaponType.Knife,"knife selected by actual slot input"))yield break;
            var grip=knife.GetComponent<FirstPersonKnifeGrip>();
            yield return new WaitForEndOfFrame();
            if(!DeviceCheck(grip && grip.GripError<.001f,"actual first-person knife grip"))yield break;
            var body=knife.GetComponentsInChildren<MeshFilter>().First().transform;
            if(!DeviceCheck(Vector3.Dot(body.right,NetworkManager.Instance.MyPlayer.GetComponent<PlayerCameraController>().PlayerCamera.transform.forward)>.7f,"knife cutting edge forward, spine toward player"))yield break;
            yield return SavePickFrame("Knife-Grip-Live");knife.OnUse();yield return new WaitForSecondsRealtime(.12f);yield return SavePickFrame("Knife-Swing-Live");
            yield return ReportKey(keyboard,UnityEngine.InputSystem.Key.Digit2);
            } finally {InputSystem.RemoveDevice(keyboard);foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);InputSystem.settings.backgroundBehavior=background;}
        }
        yield return VerifyDeviceSlot();if(finished)yield break;
        yield return DeviceReady(".pick-spawn-pass");if(finished)yield break;
        Finish("PASS: pick exchange UI, authoritative swap, private stats denial, game spawn and device slot");
    }
    IEnumerator VerifyDeviceSlot()
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;
        if(me.Team!=0 || me.SlotIndex!=0)yield break;
        var bg=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        var physical=InputSystem.devices.Where(d=>d.enabled && (d is Keyboard || d is Mouse)).ToArray();foreach(var d in physical)InputSystem.DisableDevice(d);
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try {
            Screen.SetResolution(1920,1080,false);yield return new WaitForSecondsRealtime(.5f);
            var ps=me.GetComponent<PlayerSpike>();var item=me.GetComponent<PlayerItem>();var visual=me.GetComponent<SpikeInteractionPresentation>();
            yield return DevicePosition(FindFirstObjectByType<MapInfo>().spikeSpawnPoint.position);if(finished)yield break;
            net.SendSpikePickupRequest();yield return Wait(()=>ps.hasSpike,"real device acquisition",5);if(finished)yield break;
            yield return ReportKey(keyboard,UnityEngine.InputSystem.Key.Digit4);yield return new WaitForSecondsRealtime(.5f);
            if(!DeviceCheck(item.CurrentIndex==4 && visual.IsCarryingDevice && visual.firstPerson.device.activeInHierarchy,"slot four displays held device"))yield break;
            // The authored hand/device pose is applied in LateUpdate.
            yield return SavePickFrame("Device-Slot4-Live");
            yield return new WaitForEndOfFrame();
            var cam=me.GetComponent<PlayerCameraController>().PlayerCamera;
            if(!DeviceCheck(visual.firstPerson.device.GetComponentsInChildren<Renderer>().Any(r=>{var p=cam.WorldToViewportPoint(r.bounds.center);return r.enabled&&p.z>0&&p.x>0&&p.x<1&&p.y>0&&p.y<1;}),"device visible in actual player camera"))yield break;
            yield return SavePickFrame("Device-Slot4-Live");
            yield return ReportKey(keyboard,UnityEngine.InputSystem.Key.Digit2);
            if(!DeviceCheck(!visual.IsCarryingDevice && !visual.firstPerson.device.activeSelf && item.GetCurrentWeapon()!=null,"device clears on weapon return"))yield break;
            yield return ReportKey(keyboard,UnityEngine.InputSystem.Key.Digit4);
            yield return ReportKey(keyboard,UnityEngine.InputSystem.Key.G);yield return Wait(()=>!ps.hasSpike,"server device drop",5);if(finished)yield break;
            yield return ReportKey(keyboard,UnityEngine.InputSystem.Key.Digit4);
            if(!DeviceCheck(item.CurrentIndex!=4 && !visual.firstPerson.device.activeSelf,"slot four cannot show unowned device"))yield break;
            File.WriteAllText(Path.Combine(root,"device-slot.pass"),"PASS acquired device, real key 4 carry, key 2 restore, key G drop, unowned key 4 blocked");
        } finally {InputSystem.RemoveDevice(keyboard);foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);InputSystem.settings.backgroundBehavior=bg;}
    }
}
#endif
