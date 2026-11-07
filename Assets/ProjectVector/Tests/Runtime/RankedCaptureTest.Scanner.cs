#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class RankedCaptureTest
{
    IEnumerator CaptureScannerEncounter(DronePlan plan,PlayerIdentity pilot,PlayerIdentity victim)
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;
        bool expire=Environment.GetEnvironmentVariable("VECTOR_CAPTURE_SCANNER_EXPIRE")=="1";
        if(me.PlayerId==plan.pilot){
            // Actual C and mouse input drives the authored holster/throw/recovery.
            AimActor((plan.points[0]+plan.points[1])*.5f+Vector3.up*2.7f);
            yield return Wait(()=>SkillInventoryState.CanUse(SkillSlot.C),"scanner bought",4);if(finished)yield break;
            var skill=me.GetComponent<InitiatorSkill_C>();var rig=me.GetComponent<ScannerCastRig>();var item=me.GetComponent<PlayerItem>();
            yield return PressCaptureKey(Key.C);yield return Wait(()=>rig.Ready,"scanner ready on left hand",3);if(finished)yield break;
            int ammo=item.GetCurrentWeapon().CurrentAmmo;item.GetCurrentWeapon().OnUse();Call(item,"TryStartReload");
            if(ammo!=item.GetCurrentWeapon().CurrentAmmo || item.IsReloading || GameplayActionPolicy.CanAct(item,GameplayAction.Fire)){Finish("FAILED: scanner weapon lock");yield break;}
            Evidence("scanner-ready","fire/reload blocked; authored hand model visible");
            yield return PressCaptureKey(Key.Digit2);yield return Wait(()=>!rig.Active,"cancel to weapon",3);if(finished)yield break;
            if(!SkillInventoryState.CanUse(SkillSlot.C)){Finish("FAILED: scanner cancel consumed skill");yield break;}
            Evidence("scanner-cancel","cancel retains charge");yield return new WaitForSecondsRealtime(.6f);
            yield return PressCaptureKey(Key.C);yield return Wait(()=>rig.Ready,"scanner ready again",3);if(finished)yield break;
            yield return new WaitForSecondsRealtime(1.5f);yield return ClickCaptureMouse();Evidence("scanner-thrown","mouse release");
            ScannerProjectile scanner=null;
            yield return Wait(()=>{scanner=FindObjectsByType<ScannerProjectile>(FindObjectsSortMode.None).FirstOrDefault(s=>s.OwnerPlayerId==plan.pilot && s.IsAnchored);return scanner!=null;},"server confirms floating scanner",5);if(finished)yield break;
            Evidence("scanner-anchored","position="+scanner.transform.position);
            AimActor((scanner.transform.position+victim.transform.position+Vector3.up*1.3f)*.5f);
            File.WriteAllText(Path.Combine(root,"scanner-anchored"),"go");
            yield return Wait(()=>File.Exists(Path.Combine(root,"scanner-victim-hit")),"moving target receives authoritative shock",3);if(finished)yield break;
            Evidence("scanner-hit-confirmed","victim received server stack");
            yield return Wait(()=>!rig.Active,"throw returns to gun",3);if(finished)yield break;
            var view=me.GetComponent<PlayerWeaponViewController>();
            if(!Field<GameObject>(view,"firstPersonWeaponHolder").activeInHierarchy || Field<GameObject>(view,"thirdPersonWeaponHolder").activeInHierarchy){Finish("FAILED: duplicate weapon after scanner");yield break;}
            if(!expire){yield return new WaitForSecondsRealtime(.6f);File.WriteAllText(Path.Combine(root,"scanner-destroy-now"),"go");}
            yield return Wait(()=>!scanner,"server scanner destruction or expiry",8);if(finished)yield break;
            Evidence(expire?"scanner-expired":"scanner-destroyed","projectile removed, local gun restored");
            File.WriteAllText(Path.Combine(root,"scanner-finished"),"verified");
        }else{
            int previous=me.GetComponent<ElectricShock>().ShockCount;bool hit=false;float nextShot=0;
            float end=Time.realtimeSinceStartup+25;
            while(!File.Exists(Path.Combine(root,"scanner-finished")) && Time.realtimeSinceStartup<end){
                var scanner=FindObjectsByType<ScannerProjectile>(FindObjectsSortMode.None).FirstOrDefault(s=>s.OwnerPlayerId==plan.pilot);
                if(me.PlayerId==plan.victim || me.PlayerId==plan.shooter)AimActor(scanner?scanner.transform.position:pilot.transform.position+Vector3.up*1.25f);
                if(me.PlayerId==plan.victim && scanner && scanner.IsAnchored && !hit){
                    // The existing scanner detects moving targets, not stationary ones.
                    var cc=me.GetComponent<CharacterController>();cc.Move(me.transform.right*(3f*Time.unscaledDeltaTime));
                    var p=me.transform.position;net.SendMoveState(p.x,p.y,p.z,me.transform.eulerAngles.y,0,1,false);
                    if(me.GetComponent<ElectricShock>().ShockCount>previous){
                        hit=true;Evidence("scanner-victim-hit","server stacks="+me.GetComponent<ElectricShock>().ShockCount);
                        File.WriteAllText(Path.Combine(root,"scanner-victim-hit"),"confirmed");
                    }
                }
                if(me.PlayerId==plan.shooter && scanner && File.Exists(Path.Combine(root,"scanner-destroy-now")) && Time.realtimeSinceStartup>nextShot){
                    me.GetComponent<PlayerItem>().GetCurrentWeapon().OnUse();nextShot=Time.realtimeSinceStartup+.22f;Evidence("shoot-scanner","actual weapon ray");
                }
                yield return null;
            }
            if(!File.Exists(Path.Combine(root,"scanner-finished"))){Finish("FAILED: scanner encounter timed out");yield break;}
            yield return Wait(()=>FindObjectsByType<ScannerProjectile>(FindObjectsSortMode.None).All(s=>s.OwnerPlayerId!=plan.pilot),"scanner cleanup reaches peer",3);if(finished)yield break;
            Evidence("scanner-peer-cleanup","no active scanner remains");
        }
        yield return new WaitForSecondsRealtime(3);
    }
}
#endif
