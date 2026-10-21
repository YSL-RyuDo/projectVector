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
    [Serializable] sealed class DronePlan { public uint pilot,victim,shooter; public Vector3[] points; }
    void Evidence(string name,string detail)
    {
        File.AppendAllText(Path.Combine(root,"drone-events-"+account+".csv"),DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+","+name+","+detail+"\n");
        Stage(name+" "+detail);
    }
    void AimActor(Vector3 point)
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;var cam=me.GetComponent<PlayerCameraController>().PlayerCamera;
        var direction=point-cam.transform.position;
        var rotation=Quaternion.LookRotation(direction).eulerAngles;
        var look=me.GetComponent<PlayerCameraController>().CameraLook;
        look.ResetLookRotation(rotation.y);
        typeof(FPSCameraLook).GetField("xRotation",Private).SetValue(look,Mathf.DeltaAngle(0,rotation.x));
        look.transform.localRotation=Quaternion.Euler(Mathf.DeltaAngle(0,rotation.x),0,0);
        if(cam.transform!=look.transform)cam.transform.localRotation=Quaternion.identity;
        var p=me.transform.position;
        net.SendMoveState(p.x,p.y,p.z,me.transform.eulerAngles.y,cam.transform.eulerAngles.x,0,false);
    }
    IEnumerator DriveDrone(Keyboard keyboard,Key key,float duration,Drone drone)
    {
        float end=Time.realtimeSinceStartup+duration;
        while(Time.realtimeSinceStartup<end) {
            keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;
        }
        Evidence("input-probe",$"key={key} enabled={keyboard.enabled} blocked={DroneMovement.InputBlocked} scale={Time.timeScale} target={Field<Vector3>(drone.Movement,"_targetVelocity")} velocity={drone.Movement.Velocity}");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
    }
    IEnumerator CaptureDroneEncounter()
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return Wait(()=>All(".playing"),"six clients ready for drone evidence");if(finished)yield break;
        var actors=FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        uint pilotId=uint.Parse(File.ReadAllText(Path.Combine(root,"qwer.id")));
        var pilot=actors.First(p=>p.PlayerId==pilotId);
        var enemies=actors.Where(p=>p.Team!=pilot.Team).OrderBy(p=>p.PlayerId).ToArray();
        string planFile=Path.Combine(root,"drone-plan.json");
        if(account=="qwer") {
            // Leave the crowded spawn and dropped-spike placeholder in the world,
            // but demonstrate the device in a clear, physically reachable lane.
            Vector3 travel=me.transform.forward*4;
            if(!Physics.SphereCastAll(me.transform.position+Vector3.up*1.1f,.5f,travel.normalized,travel.magnitude,~0,QueryTriggerInteraction.Ignore)
                .Any(h=>h.collider.GetComponentInParent<PlayerIdentity>()==null && h.collider.GetComponentInParent<DroppedSpikePickup>()==null)) {
                var motor=me.GetComponent<PlayerMovement>();motor.enabled=false;
                var cc=me.GetComponent<CharacterController>();cc.enabled=false;
                Vector3 destination=me.transform.position+travel;
                while(Vector3.Distance(me.transform.position,destination)>.02f) {
                    me.transform.position=Vector3.MoveTowards(me.transform.position,destination,7*Time.unscaledDeltaTime);
                    var p=me.transform.position;net.SendMoveState(p.x,p.y,p.z,me.transform.eulerAngles.y,0,0,false);yield return null;
                }
                cc.enabled=true;
            }
            var origin=me.transform.position;var points=new System.Collections.Generic.List<Vector3>();
            for(int angle=0;angle<360 && points.Count<3;angle+=30) {
                Vector3 direction=Quaternion.Euler(0,angle,0)*Vector3.forward;
                Vector3 launch=origin+direction*1.1f-Vector3.Cross(Vector3.up,direction)*.4f+Vector3.up*1.35f;
                if(points.Count==0) {
                    var launchDelta=launch-(origin+Vector3.up*1.3f);
                    if(Physics.SphereCastAll(origin+Vector3.up*1.3f,.5f,launchDelta.normalized,launchDelta.magnitude,~0,QueryTriggerInteraction.Ignore)
                        .Any(h=>!h.collider.transform.IsChildOf(me.transform)))continue;
                    if(Physics.OverlapSphere(launch,.55f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(me.transform)))continue;
                    if(Physics.SphereCastAll(launch,.55f,Vector3.up,1.5f,~0,QueryTriggerInteraction.Ignore)
                        .Any(h=>!h.collider.transform.IsChildOf(me.transform)))continue;
                }
                Vector3 point=origin+direction*7;
                var ground=Physics.RaycastAll(point+Vector3.up*4,Vector3.down,8,~0,QueryTriggerInteraction.Ignore)
                    .Where(h=>h.collider.GetComponentInParent<PlayerIdentity>()==null && h.normal.y>.65f).OrderBy(h=>h.distance).ToArray();
                if(ground.Length==0 || Mathf.Abs(ground[0].point.y-origin.y)>2)continue;
                point.y=ground[0].point.y+.1f;
                var delta=point-origin;
                if(Physics.SphereCastAll(origin+Vector3.up*1.4f,.6f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore)
                    .Any(h=>h.collider.GetComponentInParent<PlayerIdentity>()==null))continue;
                points.Add(point);
            }
            if(points.Count<3){Finish("FAILED: no clear drone lane");yield break;}
            File.WriteAllText(planFile,JsonUtility.ToJson(new DronePlan{pilot=pilotId,victim=enemies[0].PlayerId,shooter=enemies[1].PlayerId,points=points.ToArray()}));
        }
        yield return Wait(()=>File.Exists(planFile),"find unobstructed drone encounter",5);if(finished)yield break;
        yield return new WaitForSecondsRealtime(.4f);
        var plan=JsonUtility.FromJson<DronePlan>(File.ReadAllText(planFile));
        var victim=actors.First(p=>p.PlayerId==plan.victim);
        me.GetComponent<PlayerMovement>().enabled=false;
        me.GetComponent<PlayerCameraController>().CameraLook.enabled=false;
        if(me.Team!=pilot.Team) {
            var cc=me.GetComponent<CharacterController>();cc.enabled=false;
            var destination=plan.points[Array.FindIndex(enemies,p=>p.PlayerId==me.PlayerId)];
            float deadline=Time.realtimeSinceStartup+25;
            while(Vector3.Distance(me.transform.position,destination)>.02f && Time.realtimeSinceStartup<deadline) {
                me.transform.position=Vector3.MoveTowards(me.transform.position,destination,7*Time.unscaledDeltaTime);
                var p=me.transform.position;net.SendMoveState(p.x,p.y,p.z,me.transform.eulerAngles.y,0,0,false);yield return null;
            }
            cc.enabled=true;AimActor(pilot.transform.position+Vector3.up*1.4f);
        }
        File.WriteAllText(Path.Combine(root,account+".drone-ready"),me.PlayerId.ToString());
        yield return Wait(()=>All(".drone-ready"),"drone encounter actors positioned",30);if(finished)yield break;
        if(me.PlayerId!=plan.pilot && me.PlayerId!=plan.victim && me.PlayerId!=plan.shooter)recording=false;
        Evidence("encounter-ready","id="+me.PlayerId);
        if(account=="qwer") {
            AimActor(victim.transform.position+Vector3.up*1.2f);
            var eye=me.GetComponent<PlayerCameraController>().PlayerCamera.transform.position;
            foreach(var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled && r.gameObject.activeInHierarchy && Vector3.Distance(r.bounds.center,eye)<3)) {
                string path=renderer.name;for(var parent=renderer.transform.parent;parent!=null;parent=parent.parent)path=parent.name+"/"+path;
                Debug.Log($"[DRONE NEAR VISUAL] {path} player={renderer.GetComponentInParent<PlayerIdentity>()?.PlayerId} layer={renderer.gameObject.layer} bounds={renderer.bounds} mesh={renderer.GetComponent<MeshFilter>()?.sharedMesh?.name}");
            }
            yield return Wait(()=>SkillInventoryState.CanUse(SkillSlot.Q),"drone skill purchased",4);if(finished)yield break;
            var skill=me.GetComponent<InitiatorSkill_Q>();Call(skill,"UseSkill");
            var playerItem=me.GetComponent<PlayerItem>();var held=playerItem.GetCurrentWeapon();int ammoBefore=held.CurrentAmmo;
            held.OnUse();Call(playerItem,"TryStartReload");
            if(held.CurrentAmmo!=ammoBefore || playerItem.IsReloading || GameplayActionPolicy.CanAct(playerItem,GameplayAction.Fire))
            {Finish("FAILED: weapon input escaped drone preparation");yield break;}
            if(!skill.ReturnToWeapon(playerItem.CurrentIndex)){Finish("FAILED: preparation cancel did not accept weapon key");yield break;}
            yield return Wait(()=>!InitiatorSkill_Q.IsLocalReturning && !InitiatorSkill_Q.IsLocalPreparing,"cancel preparation to gun",3);if(finished)yield break;
            Evidence("weapon-lock-confirmed","ammo unchanged and cancel returned to gun");
            Call(skill,"UseSkill");
            Evidence("prepare","left arm device");yield return new WaitForSecondsRealtime(2);
            var launchRig=me.GetComponent<DroneLaunchRig>();
            if(Field<Transform[]>(launchRig,"_handBones").Length<5) { Finish("FAILED: latest hand pose missing");yield break; }
            var device=Field<GameObject>(launchRig,"_device");
            foreach(var visibleCamera in Camera.allCameras.Where(c=>c.enabled))
                Debug.Log($"[DRONE POSE CAMERA] {visibleCamera.name} fov={visibleCamera.fieldOfView} depth={visibleCamera.depth} root={visibleCamera.transform.parent?.name} device={visibleCamera.WorldToViewportPoint(device.GetComponent<Renderer>().bounds.center)} shoulder={visibleCamera.WorldToViewportPoint(Field<Transform>(launchRig,"_upperArm").position)}");
            Call(skill,"RequestDroneDeploy");
            if(Environment.GetEnvironmentVariable("VECTOR_CAPTURE_DRONE_CANCEL")=="1") {
                if(!skill.ReturnToWeapon(playerItem.CurrentIndex) || !InitiatorSkill_Q.IsLocalReturning)
                {Finish("FAILED: pending deploy did not queue weapon return");yield break;}
                held.OnUse();Call(playerItem,"TryStartReload");
                if(held.CurrentAmmo!=ammoBefore || playerItem.IsReloading)
                {Finish("FAILED: weapon fired during pending drone cancellation");yield break;}
                yield return Wait(()=>!InitiatorSkill_Q.IsLocalReturning && !InitiatorSkill_Q.IsLocalPreparing &&
                    FindObjectsByType<Drone>(FindObjectsSortMode.None).All(d=>d.OwnerPlayerId!=me.PlayerId),"late spawn cancelled before weapon unlock",6);
                if(finished)yield break;
                Evidence("pending-cancel-confirmed","queued launch cancelled; gun restored with no owned drone");
                File.WriteAllText(Path.Combine(root,"drone-finished"),"pending cancellation verified");
                yield return new WaitForSecondsRealtime(3);yield break;
            }
            yield return Wait(()=>InitiatorSkill_Q.IsLocalDroneMode,"authoritative drone deploy",6);if(finished)yield break;
            var drone=FindObjectsByType<Drone>(FindObjectsSortMode.None).First(d=>d.OwnerPlayerId==me.PlayerId);
            if(!drone.GetComponent<DroneCombatVfx>() || !Field<DroneCombatVfx>(drone.TaserGun,"_combatVfx"))
            {Finish("FAILED: latest combat effects missing");yield break;}
            Evidence("latest-build-confirmed","open hand pose and authored combat effects loaded");
            Evidence("flight-start","drone="+drone.DroneId);
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try {
                Vector3 start=drone.transform.position;
                yield return DriveDrone(keyboard,Key.Space,.25f,drone);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.6f);
                yield return DriveDrone(keyboard,Key.W,.4f,drone);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(1);
                yield return DriveDrone(keyboard,Key.D,.2f,drone);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.6f);
                yield return DriveDrone(keyboard,Key.LeftCtrl,.15f,drone);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.6f);
                if(Vector3.Distance(start,drone.transform.position)<.5f){Finish("FAILED: scripted inputs did not move drone");yield break;}
                Evidence("flight-confirmed",Vector3.Distance(start,drone.transform.position).ToString("F2")+"m");
                var body=victim.GetComponentsInChildren<HitCollider>().Where(h=>h.Part==HitPart.Body)
                    .OrderByDescending(h=>h.GetComponent<Collider>().bounds.size.sqrMagnitude).FirstOrDefault();
                for(int aim=0;aim<15;aim++) {
                    var target=body?body.GetComponent<Collider>().bounds.center:victim.transform.position+Vector3.up*1.15f;
                    var direction=target-drone.ViewTransform.position;
                    var rotation=Quaternion.LookRotation(direction).eulerAngles;
                    typeof(DroneMovement).GetField("_yaw",Private).SetValue(drone.Movement,rotation.y);
                    typeof(DroneMovement).GetField("_pitch",Private).SetValue(drone.Movement,Mathf.DeltaAngle(0,rotation.x));
                    yield return null;
                }
                if(!WeaponShotTrace.FirstHit(new Ray(drone.ViewTransform.position,drone.ViewTransform.forward),100,~0,me,drone.transform,out var aimHit) || aimHit.collider.GetComponentInParent<PlayerIdentity>()!=victim)
                {Evidence("aim-diagnostic",$"body={body?.name} center={body?.GetComponent<Collider>().bounds.center} camera={drone.ViewTransform.position} forward={drone.ViewTransform.forward} first={aimHit.collider?.name}");Finish("FAILED: taser crosshair does not cover victim hitbox");yield break;}
                Call(drone.TaserGun,"FireTaserRequest");Evidence("taser-fired","target="+plan.victim);
                yield return Wait(()=>File.Exists(Path.Combine(root,"drone-victim-hit")),"victim confirms server HP and shock",6);if(finished)yield break;
                yield return new WaitForSecondsRealtime(4);
                File.WriteAllText(Path.Combine(root,"drone-destroy-now"),"go");
                yield return Wait(()=>!InitiatorSkill_Q.IsLocalDroneMode,"enemy weapon destroys drone",15);if(finished)yield break;
                Evidence("pilot-returned","camera="+me.GetComponent<PlayerCameraController>().PlayerCamera.enabled);
                if(!me.GetComponent<PlayerCameraController>().PlayerCamera.enabled){Finish("FAILED: pilot camera not restored");yield break;}
                File.WriteAllText(Path.Combine(root,"drone-finished"),"verified");
            } finally {InputSystem.RemoveDevice(keyboard);}
        } else {
            int hp=me.GetComponent<PlayerHP>().CurrentHP;bool confirmed=false;float nextShot=0;
            float end=Time.realtimeSinceStartup+50;
            while(!File.Exists(Path.Combine(root,"drone-finished")) && Time.realtimeSinceStartup<end) {
                var drone=FindObjectsByType<Drone>(FindObjectsSortMode.None).FirstOrDefault(d=>d.OwnerPlayerId==plan.pilot);
                if(drone && (me.PlayerId==plan.victim || me.PlayerId==plan.shooter))AimActor(drone.transform.position);
                if(me.PlayerId==plan.victim && !confirmed && me.GetComponent<PlayerHP>().CurrentHP<hp && me.GetComponent<ElectricShock>().ShockCount>0) {
                    confirmed=true;Evidence("victim-hit","hp="+me.GetComponent<PlayerHP>().CurrentHP+" shock="+me.GetComponent<ElectricShock>().ShockCount);
                    File.WriteAllText(Path.Combine(root,"drone-victim-hit"),"authoritative damage/status received");
                }
                if(me.PlayerId==plan.shooter && drone && File.Exists(Path.Combine(root,"drone-destroy-now")) && Time.realtimeSinceStartup>nextShot) {
                    var weapon=me.GetComponent<PlayerItem>().GetCurrentWeapon();
                    weapon.OnUse();nextShot=Time.realtimeSinceStartup+.6f;Evidence("shoot-drone","hp="+drone.CurrentHp);
                }
                yield return null;
            }
            if(!File.Exists(Path.Combine(root,"drone-finished"))){Finish("FAILED: drone encounter completion missing");yield break;}
            yield return Wait(()=>FindObjectsByType<Drone>(FindObjectsSortMode.None).All(d=>d.OwnerPlayerId!=plan.pilot),"server destruction reaches peer",4);if(finished)yield break;
            Evidence("peer-destroy-confirmed","active drones="+FindObjectsByType<Drone>(FindObjectsSortMode.None).Length);
        }
        yield return new WaitForSecondsRealtime(4);
    }
}
#endif
