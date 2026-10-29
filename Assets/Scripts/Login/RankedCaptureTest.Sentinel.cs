#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed partial class RankedCaptureTest
{
    bool SentinelEvidence=>Environment.GetEnvironmentVariable("VECTOR_CAPTURE_SENTINEL")=="1";
    [Serializable] sealed class SentinelPlan { public uint owner,victim,trigger; public Vector3 wall,normal,target,hiddenTarget,escape; }
    readonly ConcurrentDictionary<uint,int> sentinelReveals=new();
    readonly ConcurrentQueue<string> sentinelPackets=new();
    readonly ConcurrentDictionary<ushort,int> sentinelCounts=new();
    void ObserveSentinelPacket(ushort type,byte[] body)
    {
        if(type==409 && body.Length==12)sentinelReveals.AddOrUpdate(BitConverter.ToUInt32(body,4),1,(_,n)=>n+1);
        if((type>=300&&type<=323)||(type>=407&&type<=411)||type==121||type==397){
            sentinelCounts.AddOrUpdate(type,1,(_,n)=>n+1);
            sentinelPackets.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+","+type+","+Convert.ToBase64String(body));
        }
    }
    void SentinelEvidenceEvent(string name,string detail="")
    {File.AppendAllText(Path.Combine(root,"sentinel-events-"+account+".csv"),DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+","+name+","+detail+"\n");Stage(name+" "+detail);}
    void SentinelCameraDiagnostic(string label)
    {
        var cam=Camera.main;var me=NetworkManager.Instance.MyPlayer;
        var ray=cam.ViewportPointToRay(new Vector3(.5f,.5f));
        string hits=string.Join(";",Physics.RaycastAll(ray,10,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Select(h=>h.collider.name+"@"+h.distance+" owner="+h.collider.GetComponentInParent<PlayerIdentity>()?.PlayerId));
        string near=string.Join(";",FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.forceRenderingOff&&r.bounds.SqrDistance(cam.transform.position)<.16f).Select(r=>r.name+" root="+r.transform.root.name+" bounds="+r.bounds));
        File.AppendAllText(Path.Combine(root,"sentinel-camera-"+account+".txt"),label+" camera="+cam.transform.position+" forward="+cam.transform.forward+" root="+me.transform.position+"\nHITS "+hits+"\nNEAR "+near+"\n");
    }
    IEnumerator SentinelTelemetry()
    {
        using(var log=new StreamWriter(Path.Combine(root,"sentinel-state-"+account+".csv")))
        using(var packets=new StreamWriter(Path.Combine(root,"sentinel-packets-"+account+".csv"))){
            log.WriteLine("utc,hp,slowed,speed,concussion,stunned,x,y,z");
            while(!finished){
                while(sentinelPackets.TryDequeue(out var entry))packets.WriteLine(entry);
                var actor=NetworkManager.Instance?.MyPlayer;
                if(actor){var movement=actor.GetComponent<PlayerMovement>();var p=actor.transform.position;
                    log.WriteLine(FormattableString.Invariant($"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},{actor.GetComponent<PlayerHP>().CurrentHP},{movement.IsSlowed},{movement.PlanarSpeed:F4},{SentinelFeedbackUI.Instance?.ConcussionRemaining??0:F4},{movement.IsServerStunned},{p.x:F3},{p.y:F3},{p.z:F3}"));}
                log.Flush();packets.Flush();yield return null;
            }
        }
    }
    IEnumerator SentinelMouse(float seconds,bool right=false)
    {
        var mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
        try{InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(right?MouseButton.Right:MouseButton.Left));yield return new WaitForSecondsRealtime(seconds);
            InputSystem.QueueStateEvent(mouse,new MouseState());yield return new WaitForSecondsRealtime(.12f);}
        finally{InputSystem.RemoveDevice(mouse);}
    }
    IEnumerator SentinelWalk(string label)
    {
        var actor=NetworkManager.Instance.MyPlayer;var movement=actor.GetComponent<PlayerMovement>();movement.enabled=true;
        // Movement integrates Time.deltaTime (clamped during a frame stall). Wall-clock
        // time plus key-release waits incorrectly underreported the baseline on cold frames.
        // Sample actual collision-resolved displacement on pressed frames, on the same clock.
        var input=InputSystem.AddDevice<Keyboard>();input.MakeCurrent();
        float simulated=0,distance=0,start=Time.realtimeSinceStartup;int frames=0;
        var previous=actor.transform.position;
        try{
            InputSystem.QueueStateEvent(input,new KeyboardState(Key.A));
            while(simulated<.55f && Time.realtimeSinceStartup-start<5){
                yield return new WaitForEndOfFrame();
                var current=actor.transform.position;
                if(input.aKey.isPressed){
                    distance+=Vector3.ProjectOnPlane(current-previous,Vector3.up).magnitude;
                    simulated+=Time.deltaTime;frames++;
                }
                previous=current;
            }
            InputSystem.QueueStateEvent(input,new KeyboardState());
            yield return null;
        }finally{InputSystem.RemoveDevice(input);movement.enabled=false;}
        if(simulated<.55f){Finish("FAILED: movement sample did not complete");yield break;}
        float speed=distance/simulated;
        File.WriteAllText(Path.Combine(root,account+"."+label),speed.ToString(CultureInfo.InvariantCulture));
        SentinelEvidenceEvent(label,FormattableString.Invariant($"speed={speed:F3};distance={distance:F4};simulation={simulated:F4};wall={Time.realtimeSinceStartup-start:F4};frames={frames}"));
    }
    IEnumerator CaptureSentinelEncounter()
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;var motor=me.GetComponent<PlayerMovement>();
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return Wait(()=>All(".playing"),"six real clients loaded");if(finished)yield break;
        var actors=FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        uint ownerId=uint.Parse(File.ReadAllText(Path.Combine(root,"qwer.id")));
        var owner=actors.First(p=>p.PlayerId==ownerId);
        var victim=actors.Where(p=>p.Team!=owner.Team).OrderBy(p=>p.PlayerId).First();
        motor.enabled=false;me.GetComponent<PlayerCameraController>().CameraLook.enabled=false;
        var client=Field<NetworkClient>(net,"client");client.OnPacket+=ObserveSentinelPacket;
        StartCoroutine(SentinelTelemetry());
        string planPath=Path.Combine(root,"sentinel-plan.json");
        if(account=="qwer"){
            if(!me.GetComponent<SentinelSkill_C>()){Finish("FAILED: owner is not Sentinel");yield break;}
            // Walk out of the crowded spawn using the actual movement controller before filming.
            motor.enabled=true;yield return PressCaptureKey(Key.W,.65f);motor.enabled=false;
            var o=me.transform.position;RaycastHit wall=default;bool found=false;
            bool wallLesson=Environment.GetEnvironmentVariable("VECTOR_CAPTURE_SENTINEL_LESSON")=="C";
            Vector3 hiddenTarget=default,escape=default;
            // Prefer an off-axis wall so a spawn gate and the other actors do not obscure the demonstration.
            int firstAngle=wallLesson?45:0;
            for(int a=firstAngle;a<firstAngle+360&&!found;a+=15){
                var direction=Quaternion.Euler(0,a,0)*Vector3.forward;
                foreach(var hit in Physics.RaycastAll(o+Vector3.up*1.3f,direction,8,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
                    if(hit.collider.GetComponentInParent<PlayerIdentity>()||hit.distance<3||Mathf.Abs(hit.normal.y)>.1f)continue;
                    if(wallLesson){
                        Vector3 behind=hit.point-hit.normal*2;
                        var groundHits=Physics.RaycastAll(behind+Vector3.up*2,Vector3.down,7,~0,QueryTriggerInteraction.Ignore)
                            .Where(h=>!h.collider.GetComponentInParent<PlayerIdentity>()).OrderBy(h=>h.distance).ToArray();
                        if(groundHits.Length==0||groundHits[0].normal.y<.9f)continue;
                        behind=groundHits[0].point+Vector3.up*.08f;
                        if(Mathf.Abs(behind.y-o.y)>1.5f)continue;
                        if(Physics.OverlapCapsule(behind+Vector3.up*.5f,behind+Vector3.up*1.5f,.3f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.GetComponentInParent<PlayerIdentity>()))continue;
                        var sight=behind+Vector3.up*1.2f-(o+Vector3.up*1.45f);
                        if(!Physics.RaycastAll(o+Vector3.up*1.45f,sight.normalized,sight.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.GetComponentInParent<PlayerIdentity>()))continue;
                        bool exitFound=false;
                        var tangent=Vector3.Cross(Vector3.up,hit.normal).normalized;
                        for(float distance=.75f;distance<=7f&&!exitFound;distance+=.5f)foreach(float side in new[]{-1f,1f}){
                            Vector3 candidate=behind+tangent*distance*side;
                            Vector3 from=hit.point+hit.normal*3;from.y=behind.y+1;
                            var delta=candidate+Vector3.up-from;
                            if(Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.GetComponentInParent<PlayerIdentity>()))continue;
                            if(Physics.CapsuleCastAll(behind+Vector3.up*.5f,behind+Vector3.up*1.5f,.3f,tangent*side,distance,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.GetComponentInParent<PlayerIdentity>()))continue;
                            if(!Physics.Raycast(candidate+Vector3.up,Vector3.down,3,~0,QueryTriggerInteraction.Ignore))continue;
                            Vector3 cover=candidate-tangent*side*1.25f;
                            var covered=cover+Vector3.up-from;
                            if(!Physics.RaycastAll(from,covered.normalized,covered.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.GetComponentInParent<PlayerIdentity>()))continue;
                            // Start close to the corner: return uses normal movement within the 2.5 s reveal.
                            behind=cover;escape=candidate;exitFound=true;break;
                        }
                        if(!exitFound)continue;
                        hiddenTarget=behind;
                    }
                    wall=hit;found=true;break;
                }
            }
            if(!found){Finish("FAILED: no nearby installable wall");yield break;}
            Vector3 target=wall.point+wall.normal*3;target.y=o.y;
            uint trigger=wallLesson?actors.Where(p=>p.Team!=owner.Team&&p.PlayerId!=victim.PlayerId).OrderBy(p=>p.PlayerId).First().PlayerId:0;
            File.WriteAllText(planPath,JsonUtility.ToJson(new SentinelPlan{owner=ownerId,victim=victim.PlayerId,trigger=trigger,wall=wall.point,normal=wall.normal,target=target,hiddenTarget=hiddenTarget,escape=escape}));
        }
        yield return Wait(()=>File.Exists(planPath),"choose actual map wall",8);if(finished)yield break;
        yield return new WaitForSecondsRealtime(.5f);
        var plan=JsonUtility.FromJson<SentinelPlan>(File.ReadAllText(planPath));
        if(me.PlayerId==plan.victim||me.PlayerId==plan.trigger){
            Vector3 destination=me.PlayerId==plan.victim&&plan.trigger!=0?plan.hiddenTarget:plan.target;
            // Encounter staging only, at ordinary movement speed. No server position/state override.
            var cc=me.GetComponent<CharacterController>();cc.enabled=false;
            float deadline=Time.realtimeSinceStartup+35;
            while(Vector3.Distance(me.transform.position,destination)>.05f&&Time.realtimeSinceStartup<deadline){
                me.transform.position=Vector3.MoveTowards(me.transform.position,destination,5*Time.unscaledDeltaTime);
                var p=me.transform.position;net.SendMoveState(p.x,p.y,p.z,me.transform.eulerAngles.y,0,0,false);yield return null;
            }
            cc.enabled=true;
            if(Vector3.Distance(me.transform.position,destination)>.5f){Finish("FAILED: victim staging rejected");yield break;}
        }
        AimActor(me.PlayerId==ownerId?victim.transform.position+Vector3.up:owner.transform.position+Vector3.up);
        File.WriteAllText(Path.Combine(root,account+".sentinel-ready"),"ready");
        yield return Wait(()=>All(".sentinel-ready"),"participants at encounter",40);if(finished)yield break;
        dischargeCaptureReady=true;
        if(me.PlayerId!=ownerId&&me.PlayerId!=plan.victim)recording=false;
        SentinelEvidenceEvent("encounter-ready");
        string lesson=Environment.GetEnvironmentVariable("VECTOR_CAPTURE_SENTINEL_LESSON");
        if(!string.IsNullOrEmpty(lesson)){
            yield return CaptureSentinelLesson(lesson,plan,owner,victim,me,motor);
            client.OnPacket-=ObserveSentinelPacket;yield break;
        }
        if(me.PlayerId==plan.victim){
            yield return SentinelWalk("baseline-speed");
            File.WriteAllText(Path.Combine(root,"victim-baseline-ready"),account);
            yield return Wait(()=>motor.IsSlowed,"victim receives Q slow",35);if(finished)yield break;
            yield return SentinelWalk("q-slow-speed");
            float baseline=float.Parse(File.ReadAllText(Path.Combine(root,account+".baseline-speed")),CultureInfo.InvariantCulture);
            float slow=float.Parse(File.ReadAllText(Path.Combine(root,account+".q-slow-speed")),CultureInfo.InvariantCulture);
            if(baseline<.5f||slow>=baseline*.8f){Finish("FAILED: Q actual movement was not slower");yield break;}
            File.WriteAllText(Path.Combine(root,"victim-q-confirmed"),"movement ratio="+(slow/baseline));
            yield return Wait(()=>SentinelFeedbackUI.LocalConcussed,"victim receives conditional E concussion",8);if(finished)yield break;
            float onset=Time.realtimeSinceStartup;
            if(motor.IsServerStunned||!GameplayActionPolicy.CanAct(me,GameplayAction.Fire)){Finish("FAILED: concussion incorrectly blocks fire");yield break;}
            SentinelEvidenceEvent("E-concussion","remaining="+SentinelFeedbackUI.Instance.ConcussionRemaining);
            AimActor(me.transform.position+plan.normal*2);
            var weapon=me.GetComponent<PlayerItem>().GetCurrentWeapon();int ammo=weapon.CurrentAmmo;
            yield return ClickCaptureMouse();
            if(weapon.CurrentAmmo>=ammo){Finish("FAILED: cannot fire during concussion");yield break;}
            yield return Wait(()=>!SentinelFeedbackUI.LocalConcussed,"concussion expires",5);if(finished)yield break;
            float duration=Time.realtimeSinceStartup-onset;
            if(duration<2.5f||duration>3.7f){Finish("FAILED: concussion duration "+duration);yield break;}
            SentinelEvidenceEvent("E-recovered","seconds="+duration);
            File.WriteAllText(Path.Combine(root,"victim-e-confirmed"),"duration="+duration+" fire allowed");
            yield return Wait(()=>!motor.IsSlowed,"Q slow expires",10);if(finished)yield break;
            AimActor(owner.transform.position+Vector3.up);
            File.WriteAllText(Path.Combine(root,"victim-x-ready"),"ready");
            int hp=me.GetComponent<PlayerHP>().CurrentHP;
            yield return Wait(()=>me.GetComponent<PlayerHP>().CurrentHP<hp,"server X damage reaches victim",15);if(finished)yield break;
            SentinelEvidenceEvent("X-hit","hp="+me.GetComponent<PlayerHP>().CurrentHP);
            if(!motor.IsSlowed){Finish("FAILED: X damage without enemy slow");yield break;}
            File.WriteAllText(Path.Combine(root,"victim-x-confirmed"),"server HP loss and local slow");
            yield return Wait(()=>File.Exists(Path.Combine(root,"sentinel-finished")),"owner completes live checks",25);if(finished)yield break;
        }else if(me.PlayerId==ownerId){
            yield return Wait(()=>File.Exists(Path.Combine(root,"victim-baseline-ready")),"baseline measured",10);if(finished)yield break;
            yield return Wait(()=>SkillInventoryState.CanUse(SkillSlot.C)&&SkillInventoryState.CanUse(SkillSlot.Q)&&SkillInventoryState.CanUse(SkillSlot.E)&&SkillInventoryState.CanUse(SkillSlot.X),"purchased all four skills",5);if(finished)yield break;
            AimActor(victim.transform.position+Vector3.up*.5f);
            SentinelEvidenceEvent("C-input","C then short throw RMB");yield return PressCaptureKey(Key.C);yield return SentinelMouse(.12f,true);
            yield return Wait(()=>sentinelCounts.ContainsKey(305),"server C scan",12);if(finished)yield break;
            if(!sentinelCounts.ContainsKey(303)||!sentinelCounts.ContainsKey(407)){Finish("FAILED: missing anchor/countdown");yield break;}
            var placed=FindObjectsByType<Sentinel_Trap>(FindObjectsSortMode.None).FirstOrDefault(t=>t.OwnerPlayerId==ownerId);
            if(!placed||Vector3.Distance(placed.transform.position,Camera.main.transform.position)<1){Finish("FAILED: C anchored against the caster instead of map");yield break;}
            SentinelEvidenceEvent("C-confirmed");AimActor(victim.transform.position+Vector3.up);yield return new WaitForSecondsRealtime(3);SentinelCameraDiagnostic("after-C");
            bool floorFound=false;
            for(int angle=0;angle<360&&!floorFound;angle+=30){
                var candidate=me.transform.position+Quaternion.Euler(0,angle,0)*Vector3.forward*2.5f-Vector3.up;
                AimActor(candidate);yield return null;Physics.SyncTransforms();var cam=Camera.main;
                var hits=Physics.RaycastAll(cam.ScreenPointToRay(new Vector3(Screen.width*.5f,Screen.height*.5f)),10,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.transform.IsChildOf(me.transform)).OrderBy(h=>h.distance).ToArray();
                if(hits.Length>0){var floorHit=hits[0];SentinelEvidenceEvent("Q-ray-check",floorHit.collider.name+" normal="+floorHit.normal);
                    if(floorHit.normal.y>.95f&&floorHit.collider.GetComponentInParent<PlayerIdentity>()==null){floorFound=true;SentinelEvidenceEvent("Q-ground-ray",floorHit.collider.name);}}
            }
            if(!floorFound){Finish("FAILED: no visible Q ground");yield break;}
            SentinelEvidenceEvent("Q-input");yield return PressCaptureKey(Key.Q);
            yield return Wait(()=>File.Exists(Path.Combine(root,"victim-q-confirmed")),"Q victim movement measured",6);if(finished)yield break;
            if(motor.IsSlowed){Finish("FAILED: Q slowed friendly owner");yield break;}
            SentinelEvidenceEvent("Q-confirmed","enemy slowed; friendly owner unaffected");
            AimActor(victim.transform.position+Vector3.up);yield return new WaitForSecondsRealtime(1.2f);
            AimActor(plan.wall);
            SentinelEvidenceEvent("E-input");yield return PressCaptureKey(Key.E);SentinelCameraDiagnostic("before-E-click");yield return ClickCaptureMouse();
            yield return Wait(()=>File.Exists(Path.Combine(root,"victim-e-confirmed")),"E target recovers",9);if(finished)yield break;
            yield return Wait(()=>File.Exists(Path.Combine(root,"victim-x-ready")),"ready for X",10);if(finished)yield break;
            AimActor(victim.transform.position+Vector3.up);
            SentinelEvidenceEvent("X-input","hold LMB for real charge");yield return PressCaptureKey(Key.X);yield return SentinelMouse(1.9f);
            yield return Wait(()=>File.Exists(Path.Combine(root,"victim-x-confirmed")),"X authoritative hit",8);if(finished)yield break;
            if(!SentinelCNetworkManager.Instance.IsSkillProtected(ownerId)){Finish("FAILED: no X protection");yield break;}
            SentinelEvidenceEvent("X-confirmed","protected owner HP="+me.GetComponent<PlayerHP>().CurrentHP);
            yield return new WaitForSecondsRealtime(6);
            if(me.GetComponent<PlayerHP>().CurrentHP!=100){Finish("FAILED: friendly X damage");yield break;}
            yield return Wait(()=>!SentinelCNetworkManager.Instance.IsSkillProtected(ownerId),"protection expires",7);if(finished)yield break;
            AimActor(me.transform.position+me.transform.forward*2);
            int ammo=me.GetComponent<PlayerItem>().GetCurrentWeapon().CurrentAmmo;yield return ClickCaptureMouse();
            if(me.GetComponent<PlayerItem>().GetCurrentWeapon().CurrentAmmo>=ammo){Finish("FAILED: firearm did not recover");yield break;}
            SentinelEvidenceEvent("weapon-recovered");
            File.WriteAllText(Path.Combine(root,"sentinel-finished"),"verified");
        }else{
            yield return Wait(()=>File.Exists(Path.Combine(root,"sentinel-finished")),"observe Sentinel encounter",100);if(finished)yield break;
        }
        yield return new WaitForSecondsRealtime(2);
        client.OnPacket-=ObserveSentinelPacket;
    }

    IEnumerator SentinelLessonGround(PlayerIdentity me)
    {
        for(int angle=0;angle<360;angle+=30){
            AimActor(me.transform.position+Quaternion.Euler(0,angle,0)*Vector3.forward*2.5f-Vector3.up);
            yield return null;Physics.SyncTransforms();var cam=Camera.main;
            var hits=Physics.RaycastAll(cam.ViewportPointToRay(new Vector3(.5f,.5f)),10,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>!h.collider.transform.IsChildOf(me.transform)).OrderBy(h=>h.distance).ToArray();
            if(hits.Length>0&&hits[0].normal.y>.95f&&!hits[0].collider.GetComponentInParent<PlayerIdentity>())yield break;
        }
        Finish("FAILED: no visible lesson Q ground");
    }

    IEnumerator SentinelWalkTo(Vector3 destination)
    {
        var actor=NetworkManager.Instance.MyPlayer;var motor=actor.GetComponent<PlayerMovement>();
        motor.enabled=true;var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
        float deadline=Time.realtimeSinceStartup+6;
        try{
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
            while(Vector3.ProjectOnPlane(destination-actor.transform.position,Vector3.up).magnitude>.18f && Time.realtimeSinceStartup<deadline){AimActor(destination+Vector3.up*1.4f);yield return null;}
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.12f);
            if(Vector3.ProjectOnPlane(destination-actor.transform.position,Vector3.up).magnitude>.4f)Finish("FAILED: real movement could not reach cover point");
        }finally{InputSystem.RemoveDevice(keyboard);motor.enabled=false;}
    }
    // Each lesson runs in a fresh real match: no cooldown resets or fabricated statuses.
    IEnumerator CaptureSentinelLesson(string lesson,SentinelPlan plan,PlayerIdentity owner,PlayerIdentity victim,PlayerIdentity me,PlayerMovement motor)
    {
        if(!new[]{"C","Q","E","QE","X"}.Contains(lesson)){Finish("FAILED: unknown Sentinel lesson");yield break;}
        bool caster=me.PlayerId==plan.owner,target=me.PlayerId==plan.victim;
        string ready=Path.Combine(root,"lesson-target-ready"),done=Path.Combine(root,"lesson-target-done"),end=Path.Combine(root,"sentinel-finished");
        if(target){
            if(lesson=="Q"||lesson=="E"||lesson=="QE")yield return SentinelWalk("baseline-speed");
            AimActor(owner.transform.position+Vector3.up);
            File.WriteAllText(ready,"ready");
            if(lesson=="C"){
                yield return Wait(()=>sentinelCounts.ContainsKey(305),"C target receives scan",15);if(finished)yield break;
                yield return new WaitForSecondsRealtime(1);
                if(sentinelReveals.ContainsKey(me.PlayerId)){Finish("FAILED: C hit through cover");yield break;}
                SentinelEvidenceEvent("C-blocked","wave arrived but wall blocked the hit");
                File.WriteAllText(Path.Combine(root,"c-blocked"),"pass");
                // The thrown trap may land away from the planning ray, near the floor.
                // Recheck the actual wave origin, not the higher hypothetical eye line.
                var actualTrap=FindObjectsByType<Sentinel_Trap>(FindObjectsSortMode.None).First(t=>t.OwnerPlayerId==plan.owner);
                var origin=actualTrap.transform.position+Vector3.up*.15f;
                var exitDirection=Vector3.ProjectOnPlane(plan.escape-plan.hiddenTarget,Vector3.up).normalized;
                Vector3 exposed=default;bool clearExit=false;
                for(float extra=.25f;extra<=3.25f;extra+=.25f){
                    var candidate=plan.escape+exitDirection*extra;
                    // Account for the normal movement controller's stopping tolerance.
                    var sight=candidate-exitDirection*.2f+Vector3.up-origin;
                    if(sight.magnitude>9.8f)continue;
                    bool World(Collider c)=>!c.GetComponentInParent<PlayerIdentity>()&&!c.GetComponentInParent<Sentinel_Trap>()&&!c.GetComponentInParent<SentinelDeviceVfx>();
                    if(Physics.RaycastAll(origin,sight.normalized,sight.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>World(h.collider)))continue;
                    var route=candidate-me.transform.position;
                    if(Physics.CapsuleCastAll(me.transform.position+Vector3.up*.5f,me.transform.position+Vector3.up*1.5f,.3f,route.normalized,route.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>World(h.collider)))continue;
                    if(!Physics.RaycastAll(candidate+Vector3.up,Vector3.down,2,~0,QueryTriggerInteraction.Ignore).Any(h=>World(h.collider)&&h.normal.y>.9f))continue;
                    exposed=candidate;clearExit=true;break;
                }
                if(!clearExit){Finish("FAILED: no clear walking route from actual C landing point");yield break;}
                SentinelEvidenceEvent("C-exit-route","actual trap="+origin+";exposed="+exposed);
                yield return SentinelWalkTo(exposed);if(finished)yield break;
                yield return Wait(()=>sentinelReveals.ContainsKey(me.PlayerId),"exposed target receives confirmed hit",8);if(finished)yield break;
                SentinelEvidenceEvent("C-hit","server-confirmed exposed hit");
                yield return SentinelWalkTo(plan.hiddenTarget);if(finished)yield break;
                AimActor(owner.transform.position+Vector3.up);
                SentinelEvidenceEvent("C-hide","walked behind cover after being hit");
                File.WriteAllText(Path.Combine(root,"c-hidden-after-hit"),"pass");
                if(motor.IsSlowed||me.GetComponent<PlayerHP>().CurrentHP!=100){Finish("FAILED: C applied an unintended debuff");yield break;}
            }
            if(lesson=="Q"||lesson=="E"||lesson=="QE"){
                yield return Wait(()=>motor.IsSlowed,"target receives "+lesson+" slow",15);if(finished)yield break;
                if(SentinelFeedbackUI.LocalConcussed){Finish("FAILED: initial hit unexpectedly concussed");yield break;}
                SentinelEvidenceEvent("slow-start","enemy-only slow received");
                string speedLabel=lesson=="E"?"e-slow-speed":"q-slow-speed";
                yield return SentinelWalk(speedLabel);AimActor(owner.transform.position+Vector3.up);
                float baseline=float.Parse(File.ReadAllText(Path.Combine(root,account+".baseline-speed")),CultureInfo.InvariantCulture);
                float slow=float.Parse(File.ReadAllText(Path.Combine(root,account+"."+speedLabel)),CultureInfo.InvariantCulture);
                if(baseline<.5f||slow>=baseline*.8f){Finish("FAILED: lesson did not reduce actual movement");yield break;}
                File.WriteAllText(Path.Combine(root,"lesson-slow-confirmed"),"actual movement reduced");
                if(lesson=="QE"){
                    yield return Wait(()=>SentinelFeedbackUI.LocalConcussed,"Q then E concussion",8);if(finished)yield break;
                    float onset=Time.realtimeSinceStartup;
                    SentinelEvidenceEvent("E-concussion","remaining="+SentinelFeedbackUI.Instance.ConcussionRemaining);
                    if(motor.IsServerStunned||!GameplayActionPolicy.CanAct(me,GameplayAction.Fire)){Finish("FAILED: concussion blocks actions");yield break;}
                    AimActor(me.transform.position+plan.normal*2);
                    var weapon=me.GetComponent<PlayerItem>().GetCurrentWeapon();int ammo=weapon.CurrentAmmo;
                    yield return ClickCaptureMouse();AimActor(owner.transform.position+Vector3.up);
                    if(weapon.CurrentAmmo>=ammo){Finish("FAILED: concussion blocks firing");yield break;}
                    SentinelEvidenceEvent("concussion-fire","actual ammunition consumed");
                    yield return Wait(()=>!SentinelFeedbackUI.LocalConcussed,"concussion recovers",5);if(finished)yield break;
                    float duration=Time.realtimeSinceStartup-onset;
                    if(duration<2.5f||duration>3.7f){Finish("FAILED: invalid concussion duration");yield break;}
                    SentinelEvidenceEvent("E-recovered","duration="+duration);
                }else{
                    float deadline=Time.realtimeSinceStartup+8;
                    while(motor.IsSlowed&&Time.realtimeSinceStartup<deadline){
                        if(SentinelFeedbackUI.LocalConcussed||motor.IsServerStunned){Finish("FAILED: standalone slow became concussion/stun");yield break;}
                        yield return null;
                    }
                    if(motor.IsSlowed){Finish("FAILED: standalone slow did not expire");yield break;}
                    SentinelEvidenceEvent("slow-end","normal movement restored; no concussion");
                }
                if(me.GetComponent<PlayerHP>().CurrentHP!=100){Finish("FAILED: Q/E caused damage");yield break;}
            }
            if(lesson=="X"){
                yield return Wait(()=>me.GetComponent<PlayerHP>().CurrentHP<100,"X authoritative damage",20);if(finished)yield break;
                SentinelEvidenceEvent("X-hit","hp="+me.GetComponent<PlayerHP>().CurrentHP);
                if(!motor.IsSlowed){Finish("FAILED: X enemy slow missing");yield break;}
            }
            File.WriteAllText(done,"verified");
            yield return Wait(()=>File.Exists(end),"lesson ends",25);if(finished)yield break;
        }else if(caster){
            yield return Wait(()=>File.Exists(ready),"target ready for "+lesson,12);if(finished)yield break;
            yield return Wait(()=>SkillInventoryState.CanUse(SkillSlot.C)&&SkillInventoryState.CanUse(SkillSlot.Q)&&SkillInventoryState.CanUse(SkillSlot.E)&&SkillInventoryState.CanUse(SkillSlot.X),"lesson purchases confirmed",5);if(finished)yield break;
            AimActor(victim.transform.position+Vector3.up);yield return new WaitForSecondsRealtime(1);
            SentinelEvidenceEvent("lesson-start",lesson);
            yield return new WaitForSecondsRealtime(1.5f);
            if(lesson=="C"||lesson=="X"){
                var trigger=plan.trigger==0?victim:FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).First(p=>p.PlayerId==plan.trigger);
                AimActor(trigger.transform.position+Vector3.up*.5f);SentinelEvidenceEvent("C-input","short throw RMB");
                yield return PressCaptureKey(Key.C);yield return new WaitForSecondsRealtime(.35f);
                var held=me.GetComponent<PlayerItem>().GetCurrentWeapon();
                if(held.gameObject.activeSelf||GameplayActionPolicy.CanAct(me,GameplayAction.Fire)){Finish("FAILED: Sentinel gun not holstered during selection");yield break;}
                SentinelEvidenceEvent("weapon-holstered","gun hidden and firing blocked before cast");
                yield return SentinelMouse(.12f,true);
                yield return Wait(()=>sentinelCounts.ContainsKey(305),"real C anchor and scan",12);if(finished)yield break;
                var placed=FindObjectsByType<Sentinel_Trap>(FindObjectsSortMode.None).FirstOrDefault(t=>t.OwnerPlayerId==plan.owner);
                if(!placed||Vector3.Distance(placed.transform.position,Camera.main.transform.position)<1){Finish("FAILED: C did not land on map");yield break;}
                SentinelEvidenceEvent("C-confirmed");AimActor(victim.transform.position+Vector3.up);
                if(lesson=="C"&&plan.trigger!=0){
                    var outline=victim.GetComponentInChildren<Outline>(true);
                    yield return Wait(()=>File.Exists(Path.Combine(root,"c-blocked")),"wall blocks first hit",5);if(finished)yield break;
                    if(outline && outline.OutlineMode==Outline.Mode.OutlineAndSilhouette){Finish("FAILED: hidden target revealed without hit");yield break;}
                    SentinelEvidenceEvent("C-blocked-confirmed","no silhouette through wall before a hit");
                    float deadline=Time.realtimeSinceStartup+16;
                    while(!File.Exists(Path.Combine(root,"c-hidden-after-hit"))&&Time.realtimeSinceStartup<deadline){AimActor(victim.transform.position+Vector3.up);yield return null;}
                    if(!File.Exists(Path.Combine(root,"c-hidden-after-hit"))){Finish("FAILED: target did not return behind cover");yield break;}
                    if(!outline||outline.OutlineMode!=Outline.Mode.OutlineAndSilhouette){Finish("FAILED: confirmed reveal lost behind cover");yield break;}
                    var cam=Camera.main;var sight=victim.transform.position+Vector3.up*1.2f-cam.transform.position;
                    if(!Physics.RaycastAll(cam.transform.position,sight.normalized,sight.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.GetComponentInParent<PlayerIdentity>()&&!h.collider.GetComponentInParent<Sentinel_Trap>())){Finish("FAILED: C target is not behind an actual wall");yield break;}
                    SentinelEvidenceEvent("C-wall-verified","real map wall blocks direct sight; scan enables silhouette");
                    yield return new WaitForSecondsRealtime(3);
                    if(outline.OutlineMode==Outline.Mode.OutlineAndSilhouette){Finish("FAILED: reveal did not expire behind cover");yield break;}
                    SentinelEvidenceEvent("C-expired","silhouette expired without a new hit");
                }
                yield return new WaitForSecondsRealtime(lesson=="C"?5:2);
            }
            if(lesson=="Q"||lesson=="QE"){
                yield return SentinelLessonGround(me);if(finished)yield break;
                SentinelEvidenceEvent("Q-input");yield return PressCaptureKey(Key.Q);
                yield return Wait(()=>File.Exists(Path.Combine(root,"lesson-slow-confirmed")),"Q movement measured",6);if(finished)yield break;
                if(motor.IsSlowed){Finish("FAILED: Q slowed caster");yield break;}
                SentinelEvidenceEvent("Q-confirmed","enemy slowed; friendly owner unaffected");
                AimActor(victim.transform.position+Vector3.up);yield return new WaitForSecondsRealtime(1.7f);
            }
            if(lesson=="E"||lesson=="QE"){
                AimActor(plan.wall);SentinelEvidenceEvent("E-input");yield return PressCaptureKey(Key.E);yield return new WaitForSecondsRealtime(.35f);
                if(me.GetComponent<PlayerItem>().GetCurrentWeapon().gameObject.activeSelf||GameplayActionPolicy.CanAct(me,GameplayAction.Fire)){Finish("FAILED: E gun not holstered");yield break;}
                SentinelEvidenceEvent("E-holstered","gun hidden during E selection");
                yield return ClickCaptureMouse();
            }
            if(lesson=="X"){
                SentinelEvidenceEvent("X-input","C installed beforehand; X charge starts");
                yield return PressCaptureKey(Key.X);yield return new WaitForSecondsRealtime(.35f);yield return SentinelMouse(1.9f);
                yield return Wait(()=>File.Exists(done),"real X hit",8);if(finished)yield break;
                if(!SentinelCNetworkManager.Instance.IsSkillProtected(plan.owner)){Finish("FAILED: X protection missing");yield break;}
                SentinelEvidenceEvent("X-confirmed","enemy damage/slow; caster unaffected");
                yield return Wait(()=>!SentinelCNetworkManager.Instance.IsSkillProtected(plan.owner),"X protection expires",12);if(finished)yield break;
                AimActor(me.transform.position+me.transform.forward*2);
                var weapon=me.GetComponent<PlayerItem>().GetCurrentWeapon();int ammo=weapon.CurrentAmmo;yield return ClickCaptureMouse();
                if(weapon.CurrentAmmo>=ammo){Finish("FAILED: firearm did not recover");yield break;}
                SentinelEvidenceEvent("weapon-recovered");
            }
            yield return Wait(()=>File.Exists(done),"target confirms "+lesson,12);if(finished)yield break;
            if(lesson=="QE"){
                yield return Wait(()=>!me.GetComponent<SentinelCastMotion>().Recovering,"Q/E cast recovers",3);if(finished)yield break;
                AimActor(me.transform.position+plan.normal*2);
                var weapon=me.GetComponent<PlayerItem>().GetCurrentWeapon();int ammo=weapon.CurrentAmmo;
                if(!weapon.gameObject.activeInHierarchy){Finish("FAILED: Q/E gun visibility did not recover");yield break;}
                yield return ClickCaptureMouse();
                if(weapon.CurrentAmmo>=ammo){Finish("FAILED: Q/E firearm did not recover");yield break;}
                SentinelEvidenceEvent("weapon-recovered","Q/E cast ended; gun visible and actual ammunition consumed");
                AimActor(victim.transform.position+Vector3.up);
            }
            if(motor.IsSlowed||SentinelFeedbackUI.LocalConcussed||me.GetComponent<PlayerHP>().CurrentHP!=100){Finish("FAILED: friendly caster affected");yield break;}
            yield return new WaitForSecondsRealtime(2);
            SentinelEvidenceEvent("lesson-end",lesson);File.WriteAllText(end,"verified "+lesson);
        }else{
            float deadline=Time.realtimeSinceStartup+65;
            while(!File.Exists(end)&&Time.realtimeSinceStartup<deadline){
                if(me.Team==owner.Team&&(motor.IsSlowed||SentinelFeedbackUI.LocalConcussed||me.GetComponent<PlayerHP>().CurrentHP!=100)){Finish("FAILED: ally affected by Sentinel lesson");yield break;}
                yield return null;
            }
            if(!File.Exists(end)){Finish("FAILED: lesson timeout");yield break;}
        }
        yield return new WaitForSecondsRealtime(1);
    }
}
#endif
