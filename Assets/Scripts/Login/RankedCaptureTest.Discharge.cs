#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public sealed partial class RankedCaptureTest
{
    bool DischargeEvidence => Environment.GetEnvironmentVariable("VECTOR_CAPTURE_DISCHARGE")=="1";
    bool dischargeCaptureReady;
    RenderTexture dischargeTarget;
    void CaptureDischargeFrame(Texture2D image)
    {
        // Render the running client's existing camera/HUD, including actual network events.
        // Hidden Windows swap chains return black screenshots; this bypasses only that swap chain.
        var camera=NetworkManager.Instance.MyPlayer?.GetComponent<PlayerCameraController>()?.PlayerCamera;
        if(!camera || !camera.enabled)return;
        if(!dischargeTarget)dischargeTarget=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas && c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var oldCameras=canvases.Select(c=>c.worldCamera).ToArray();
        var oldDistances=canvases.Select(c=>c.planeDistance).ToArray();
        var prior=camera.targetTexture;var active=RenderTexture.active;
        try{
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.3f;}
            camera.targetTexture=dischargeTarget;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=dischargeTarget;
            image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(frames,frame.ToString("D6")+".jpg"),image.EncodeToJPG(92));
            timeline.WriteLine(frame+","+DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());timeline.Flush();frame++;
        }finally{
            camera.targetTexture=prior;RenderTexture.active=active;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=oldCameras[i];canvases[i].planeDistance=oldDistances[i];}
        }
    }
    IEnumerator CaptureDischargeEncounter(DronePlan plan,PlayerIdentity pilot,PlayerIdentity victim)
    {
        var me=NetworkManager.Instance.MyPlayer;
        dischargeCaptureReady=true;
        if(me.PlayerId==plan.pilot){
            AimActor(victim.transform.position+Vector3.up*1.2f);
            var skill=me.GetComponent<InitiatorSkill_X>();
            var item=me.GetComponent<PlayerItem>();
            yield return Wait(()=>SkillInventoryState.CanUse(SkillSlot.X),"ultimate purchase confirmed",4);if(finished)yield break;
            yield return new WaitForSecondsRealtime(2);
            Evidence("discharge-input","actual X key; first-person camera");
            yield return PressCaptureKey(Key.X);
            if(!skill.IsCasting){Finish("FAILED: X did not enter authored gesture");yield break;}
            int ammo=item.GetCurrentWeapon().CurrentAmmo;
            item.GetCurrentWeapon().OnUse();Call(item,"TryStartReload");
            if(item.GetCurrentWeapon().CurrentAmmo!=ammo || item.IsReloading){Finish("FAILED: weapon escaped ultimate lock");yield break;}
            yield return Wait(()=>File.Exists(Path.Combine(root,"discharge-victim-hit")),"server ultimate damage and shock",7);if(finished)yield break;
            Evidence("discharge-confirmed","victim independently received HP loss and shock");
            yield return Wait(()=>!skill.IsCasting,"left hand returns to firearm",4);if(finished)yield break;
            var view=me.GetComponent<PlayerWeaponViewController>();
            if(!Field<GameObject>(view,"firstPersonWeaponHolder").activeInHierarchy || Field<GameObject>(view,"thirdPersonWeaponHolder").activeInHierarchy){Finish("FAILED: wrong weapon view after ultimate");yield break;}
            yield return new WaitForSecondsRealtime(2);
            // Fire into empty floor to prove normal weapon input has recovered.
            AimActor(me.transform.position+me.transform.forward*3);
            ammo=item.GetCurrentWeapon().CurrentAmmo;
            yield return ClickCaptureMouse();
            if(item.GetCurrentWeapon().CurrentAmmo>=ammo){Finish("FAILED: firearm did not resume after ultimate");yield break;}
            Evidence("discharge-weapon-restored","FP only; live firearm consumed ammo");
            yield return new WaitForSecondsRealtime(2);
            File.WriteAllText(Path.Combine(root,"discharge-finished"),"verified");
        }else{
            int hp=me.GetComponent<PlayerHP>().CurrentHP;
            int shock=me.GetComponent<ElectricShock>().ShockCount;
            bool hit=false;float deadline=Time.realtimeSinceStartup+20;
            while(!File.Exists(Path.Combine(root,"discharge-finished")) && Time.realtimeSinceStartup<deadline){
                AimActor(pilot.transform.position+Vector3.up*1.3f);
                if(me.PlayerId==plan.victim && !hit && me.GetComponent<PlayerHP>().CurrentHP<hp && me.GetComponent<ElectricShock>().ShockCount>shock){
                    hit=true;Evidence("discharge-victim-hit","HP="+me.GetComponent<PlayerHP>().CurrentHP+" shock="+me.GetComponent<ElectricShock>().ShockCount);
                    File.WriteAllText(Path.Combine(root,"discharge-victim-hit"),"confirmed");
                }
                yield return null;
            }
            if(!File.Exists(Path.Combine(root,"discharge-finished"))){Finish("FAILED: ultimate completion missing");yield break;}
        }
        yield return new WaitForSecondsRealtime(1);
    }
}
#endif
