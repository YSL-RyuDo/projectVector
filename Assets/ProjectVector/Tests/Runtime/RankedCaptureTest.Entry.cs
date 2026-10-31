#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed partial class RankedCaptureTest
{
    bool EntryEvidence=>Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ENTRY")=="1";
    bool EntryDashOnly=>Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ENTRY_LESSON")=="C";
    [Serializable] sealed class EntryPlan { public uint owner,victim; public Vector3 origin,direction,target; }
    [Serializable] sealed class EntryDashEndpoint { public Vector3 position; }
    readonly ConcurrentDictionary<ushort,int> entryCounts=new();
    readonly ConcurrentQueue<string> entryPackets=new();
    int EntryCount(ushort type)=>entryCounts.TryGetValue(type,out var n)?n:0;
    void ObserveEntryPacket(ushort type,byte[] body)
    {
        if(type==222 || (type>=231&&type<=253) || type==121){
            entryCounts.AddOrUpdate(type,1,(_,n)=>n+1);
            entryPackets.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+","+type+","+Convert.ToBase64String(body));
        }
    }
    void EntryEvent(string label,string detail="")
    {
        File.AppendAllText(Path.Combine(root,"entry-events-"+account+".csv"),DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+","+label+","+detail+"\n");
        Stage(label+" "+detail);
    }
    IEnumerator EntryTelemetry(PlayerIdentity owner)
    {
        using(var log=new StreamWriter(Path.Combine(root,"entry-state-"+account+".csv")))
        using(var packets=new StreamWriter(Path.Combine(root,"entry-packets-"+account+".csv"))){
            log.WriteLine("utc,hp,ownerStealth,ownerAlpha,ownerBlades,ownerRecovering,ownerX,ownerY,ownerZ,weaponActive,ownerAnim,ownerGrounded,ownerCanMove,ownerClip");
            while(!finished){
                while(entryPackets.TryDequeue(out var p))packets.WriteLine(p);
                var me=NetworkManager.Instance.MyPlayer;var e=owner.GetComponent<EntrySkill_E>();var x=owner.GetComponent<EntrySkill_X>();var r=owner.GetComponent<EntrySkillPresentation>();
                var pos=owner.transform.position;var item=me.GetComponent<PlayerItem>();var weapon=item.GetCurrentWeapon();
                var movement=owner.GetComponent<PlayerMovement>();
                var clips=owner.transform.Find("Model").GetComponent<Animator>().GetCurrentAnimatorClipInfo(0);
                log.WriteLine(FormattableString.Invariant($"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},{me.GetComponent<PlayerHP>().CurrentHP},{e.IsStealth},{Field<float>(e,"_lastRemoteAlpha"):F3},{Field<int>(x,"_bladeCount")},{r.Recovering},{pos.x:F3},{pos.y:F3},{pos.z:F3},{(weapon&&weapon.gameObject.activeInHierarchy)},{movement.GetCurrentAnimIndex()},{owner.GetComponent<CharacterController>().isGrounded},{movement.CanMove},{string.Join("|",clips.Select(c=>c.clip.name))}"));
                log.Flush();packets.Flush();yield return null;
            }
        }
    }
    IEnumerator EntryRecovery(string skill,Vector3 returnAim)
    {
        var me=NetworkManager.Instance.MyPlayer;var rig=me.GetComponent<EntrySkillPresentation>();var item=me.GetComponent<PlayerItem>();
        yield return Wait(()=>!rig.Recovering&&!EntrySkill_X.IsLocalBladeMode,"weapon return after "+skill,4);if(finished)yield break;
        yield return new WaitForSecondsRealtime(.5f);
        var gun=item.GetCurrentWeapon();
        if(!gun||!gun.gameObject.activeInHierarchy||!GameplayActionPolicy.CanAct(item,GameplayAction.Fire)){Finish("FAILED: firearm visibility/input after "+skill);yield break;}
        AimActor(me.transform.position+me.transform.right*2+me.transform.forward*2);
        int ammo=gun.CurrentAmmo;yield return ClickCaptureMouse();
        if(gun.CurrentAmmo>=ammo){Finish("FAILED: firearm ammo unchanged after "+skill);yield break;}
        EntryEvent(skill+"-weapon-return","ammo="+ammo+"->"+gun.CurrentAmmo);
        AimActor(returnAim);yield return new WaitForSecondsRealtime(1.5f);
    }
    IEnumerator CaptureEntryEncounter()
    {
        var neutral=InputSystem.AddDevice<Keyboard>();neutral.MakeCurrent();
        InputSystem.QueueStateEvent(neutral,new KeyboardState());
        try { yield return CaptureEntryEncounterCore(); }
        finally { InputSystem.RemoveDevice(neutral); }
    }
    IEnumerator CaptureEntryEncounterCore()
    {
        var net=NetworkManager.Instance;var me=net.MyPlayer;var motor=me.GetComponent<PlayerMovement>();
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return Wait(()=>All(".playing"),"six clients loaded");if(finished)yield break;
        var actors=FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        uint ownerId=uint.Parse(File.ReadAllText(Path.Combine(root,"qwer.id")));
        var owner=actors.First(p=>p.PlayerId==ownerId);var victim=actors.Where(p=>p.Team!=owner.Team).OrderBy(p=>p.PlayerId).First();
        // Keep the production movement sender alive even while staging/standing still.
        motor.enabled=true;motor.CanMove=false;me.GetComponent<PlayerCameraController>().CameraLook.enabled=false;
        var client=Field<NetworkClient>(net,"client");client.OnPacket+=ObserveEntryPacket;StartCoroutine(EntryTelemetry(owner));
        string planPath=Path.Combine(root,"entry-plan.json");
        if(account=="qwer"){
            if(!me.GetComponent<EntrySkill_X>()){Finish("FAILED: Entry not selected");yield break;}
            Vector3 spawn=me.transform.position,origin=spawn,direction=Vector3.zero,target=default;
            float laneLength=EntryDashOnly?10.5f:12;
            // Spawn sides/maps vary. Search nearby, collision-clear staging points;
            // never teleport the caster or disable its production position sender.
            for(int radius=0;radius<=9&&direction==Vector3.zero;radius+=3)
            for(int heading=0;heading<(radius==0?1:360)&&direction==Vector3.zero;heading+=15){
                var offset=Quaternion.Euler(0,heading,0)*Vector3.forward*radius;
                if(radius>0&&!EntryLaneClear(spawn,offset.normalized,radius))continue;
                var start=spawn+offset;
                for(int angle=0;angle<360;angle+=5){
                    var d=Quaternion.Euler(0,angle,0)*Vector3.forward;
                    if(!EntryLaneClear(start,d,laneLength+.5f))continue;
                    var candidate=start+d*laneLength;
                    var grounds=Physics.RaycastAll(candidate+Vector3.up*2,Vector3.down,5,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.GetComponentInParent<PlayerIdentity>()).OrderBy(h=>h.distance).ToArray();
                    if(grounds.Length==0 || grounds[0].normal.y<.9f || Mathf.Abs(grounds[0].point.y-start.y)>1)continue;
                    origin=start;direction=d;target=grounds[0].point+Vector3.up*.08f;break;
                }
            }
            if(direction==Vector3.zero){Finish("FAILED: no clear dash lane");yield break;}
            float stagingEnd=Time.realtimeSinceStartup+6;
            while(Vector3.Distance(me.transform.position,origin)>.1f&&Time.realtimeSinceStartup<stagingEnd){
                me.GetComponent<CharacterController>().Move(Vector3.ClampMagnitude(origin-me.transform.position,5*Time.deltaTime));yield return null;
            }
            if(Vector3.Distance(me.transform.position,origin)>.15f){Finish("FAILED: staging path blocked");yield break;}
            origin=me.transform.position;
            File.WriteAllText(planPath,JsonUtility.ToJson(new EntryPlan{owner=ownerId,victim=victim.PlayerId,origin=origin,direction=direction,target=target}));
        }
        yield return Wait(()=>File.Exists(planPath),"select clear lane",10);if(finished)yield break;yield return new WaitForSecondsRealtime(.5f);
        var plan=JsonUtility.FromJson<EntryPlan>(File.ReadAllText(planPath));
        if(me.PlayerId==plan.victim){
            // Stage the encounter at ordinary speed before the lesson. Skills and hits
            // below are driven through real inputs and server handlers, never fake replies.
            var cc=me.GetComponent<CharacterController>();cc.enabled=false;float end=Time.realtimeSinceStartup+40;
            while(Vector3.Distance(me.transform.position,plan.target)>.05f&&Time.realtimeSinceStartup<end){
                me.transform.position=Vector3.MoveTowards(me.transform.position,plan.target,5*Time.unscaledDeltaTime);AimActor(owner.transform.position+Vector3.up*1.3f);yield return null;
            }
            cc.enabled=true;File.WriteAllText(Path.Combine(root,"entry-target-ready"),account);
        }
        yield return Wait(()=>File.Exists(Path.Combine(root,"entry-target-ready")),"opponent staged",45);if(finished)yield break;
        // Stopping input is not stopping physics: keep grounding/gravity/animation
        // and normal replication running throughout the actual demonstration.
        motor.CanMove=true;
        yield return new WaitForSecondsRealtime(.7f);
        dischargeCaptureReady=me.PlayerId==ownerId||me.PlayerId==plan.victim;
        if(account=="qwer"){
            yield return PressCaptureKey(Key.Digit2);yield return new WaitForSecondsRealtime(.6f);
            yield return Wait(()=>new[]{SkillSlot.C,SkillSlot.Q,SkillSlot.E,SkillSlot.X}.All(SkillInventoryState.CanUse),"four skills purchased",4);if(finished)yield break;
            AimActor(plan.target+Vector3.up*1.2f);EntryEvent("lesson-start");yield return new WaitForSecondsRealtime(2);
            EntryEvent("C-start");var before=me.transform.position;EntryEvent("C-input");
            File.WriteAllText(Path.Combine(root,"entry-c-start"),"go");
            yield return PressCaptureKey(Key.C);yield return Wait(()=>EntryCount(241)>0,"server dash",3);if(finished)yield break;
            while(me.GetComponent<EntryDashMotor>().IsDashing){
                yield return new WaitForEndOfFrame();
                if(me.GetComponent<EntryDashMotor>().IsDashing&&(!me.GetComponent<CharacterController>().isGrounded||motor.GetCurrentAnimIndex()%6==5)){
                    Finish("FAILED: ground dash emitted airborne animation");yield break;
                }
            }
            yield return new WaitForSecondsRealtime(.8f);var distance=Vector3.Distance(before,me.transform.position);
            if(distance<3){Finish("FAILED: dash displacement="+distance);yield break;}
            File.WriteAllText(Path.Combine(root,"entry-c-endpoint.json"),JsonUtility.ToJson(new EntryDashEndpoint{position=me.transform.position}));
            yield return Wait(()=>File.Exists(Path.Combine(root,"entry-c-remote-pass")),"opponent continuous dash path",4);if(finished)yield break;
            AimActor(plan.target+Vector3.up*1.2f);EntryEvent("C-confirmed","distance="+distance.ToString("F2",CultureInfo.InvariantCulture));
            EntryEvent("C-idle-start");File.WriteAllText(Path.Combine(root,"entry-c-idle-start"),"go");
            yield return VerifyEntryIdle(me,"local");if(finished)yield break;
            yield return Wait(()=>File.Exists(Path.Combine(root,"entry-c-idle-pass")),"opponent grounded idle pose",5);if(finished)yield break;
            yield return EntryRecovery("C",plan.target+Vector3.up*1.2f);if(finished)yield break;
            var walkStart=me.transform.position;
            yield return PressCaptureKey(Key.S,.25f);
            if(Vector3.Distance(walkStart,me.transform.position)<.3f){Finish("FAILED: ordinary movement after C");yield break;}
            EntryEvent("C-walk-return");yield return new WaitForSecondsRealtime(1);EntryEvent("C-end");
            if(EntryDashOnly){
                EntryEvent("lesson-end");File.WriteAllText(Path.Combine(root,"entry-finished"),"PASS");
                client.OnPacket-=ObserveEntryPacket;yield return new WaitForSecondsRealtime(.5f);yield break;
            }

            EntryEvent("Q-start");yield return new WaitForSecondsRealtime(1);
            AimActor(Vector3.Lerp(me.transform.position,plan.target,.55f));EntryEvent("Q-input");yield return PressCaptureKey(Key.Q);
            yield return Wait(()=>EntryCount(233)>0,"server smoke attachment and bloom",8);if(finished)yield break;EntryEvent("Q-confirmed");
            AimActor(plan.target+Vector3.up*1.2f);yield return new WaitForSecondsRealtime(3);
            yield return Wait(()=>EntryCount(234)>0,"smoke lifetime complete",8);if(finished)yield break;
            yield return Wait(()=>File.Exists(Path.Combine(root,"entry-q-returned")),"opponent returns from exterior smoke view",5);if(finished)yield break;
            yield return EntryRecovery("Q",plan.target+Vector3.up*1.2f);if(finished)yield break;EntryEvent("Q-end");

            EntryEvent("E-start");yield return new WaitForSecondsRealtime(1);EntryEvent("E-input");yield return PressCaptureKey(Key.E);
            yield return Wait(()=>me.GetComponent<EntrySkill_E>().IsStealth,"server stealth active",3);if(finished)yield break;EntryEvent("E-confirmed");
            yield return Wait(()=>File.Exists(Path.Combine(root,"entry-hidden")),"enemy far visibility",2);if(finished)yield break;
            File.WriteAllText(Path.Combine(root,"entry-approach"),"go");
            yield return Wait(()=>File.Exists(Path.Combine(root,"entry-near")),"enemy near visibility",3);if(finished)yield break;
            yield return Wait(()=>!me.GetComponent<EntrySkill_E>().IsStealth,"stealth duration complete",6);if(finished)yield break;EntryEvent("E-expired");
            yield return EntryRecovery("E",victim.transform.position+Vector3.up*1.2f);if(finished)yield break;EntryEvent("E-end");

            EntryEvent("X-start");yield return new WaitForSecondsRealtime(1);EntryEvent("X-input");yield return PressCaptureKey(Key.X);
            var x=me.GetComponent<EntrySkill_X>();yield return Wait(()=>Field<int>(x,"_bladeCount")==6,"server six claws",3);if(finished)yield break;
            yield return new WaitForSecondsRealtime(1);
            AimActor(me.transform.position+me.transform.right*2+me.transform.forward*2);EntryEvent("X-left");yield return ClickCaptureMouse();
            yield return Wait(()=>Field<int>(x,"_bladeCount")==5,"first claw consumed",2);if(finished)yield break;yield return new WaitForSecondsRealtime(.8f);
            AimActor(victim.transform.position+Vector3.up*1.1f);EntryEvent("X-right");yield return ClickCaptureMouse();
            yield return Wait(()=>Field<int>(x,"_bladeCount")==4&&File.Exists(Path.Combine(root,"entry-hit")),"second claw and enemy damage",2);if(finished)yield break;
            yield return new WaitForSecondsRealtime(1);AimActor(victim.transform.position+Vector3.up*1.1f);EntryEvent("X-burst");yield return SentinelMouse(.08f,true);
            yield return Wait(()=>!EntrySkill_X.IsLocalBladeMode&&File.Exists(Path.Combine(root,"entry-burst-hit")),"burst depletion and enemy damage",3);if(finished)yield break;
            yield return EntryRecovery("X",victim.transform.position+Vector3.up*1.2f);if(finished)yield break;EntryEvent("X-end");
            EntryEvent("lesson-end");yield return new WaitForSecondsRealtime(.5f);File.WriteAllText(Path.Combine(root,"entry-finished"),"PASS");
        }else{
            if(me.PlayerId==plan.victim){
                yield return VerifyEntryRemoteDash(owner,plan.direction);if(finished)yield break;
                yield return Wait(()=>File.Exists(Path.Combine(root,"entry-c-idle-start")),"waiting for post-dash idle",5);if(finished)yield break;
                yield return VerifyEntryIdle(owner,"remote");if(finished)yield break;
                File.WriteAllText(Path.Combine(root,"entry-c-idle-pass"),"PASS");
            }
            bool hidden=false,near=false,hit=false,burst=false;int hp=me.GetComponent<PlayerHP>().CurrentHP;
            float smokeSeen=-1;bool outside=false,qReturned=false;
            Transform[] weaponTransforms=null;Vector3[] weaponPositions=null;Quaternion[] weaponRotations=null;
            var walking=InputSystem.AddDevice<Keyboard>();walking.MakeCurrent();
            float end=Time.realtimeSinceStartup+110;
            while(!File.Exists(Path.Combine(root,"entry-finished"))&&Time.realtimeSinceStartup<end){
                if(me.PlayerId==plan.victim){
                    AimActor(owner.transform.position+Vector3.up*1.3f);
                    if(EntryCount(233)>0&&!qReturned){
                        if(smokeSeen<0)smokeSeen=Time.realtimeSinceStartup;
                        if(EntryCount(234)==0){
                            if(Time.realtimeSinceStartup-smokeSeen>.9f&&!outside){
                                if(Vector3.Distance(me.transform.position,plan.target)<3.5f){motor.CanMove=true;InputSystem.QueueStateEvent(walking,new KeyboardState(Key.S));}
                                else {outside=true;InputSystem.QueueStateEvent(walking,new KeyboardState());EntryEvent("Q-outside");}
                            }
                        }else{
                            if(Vector3.Distance(me.transform.position,plan.target)>.25f){motor.CanMove=true;InputSystem.QueueStateEvent(walking,new KeyboardState(Key.W));}
                            else {qReturned=true;InputSystem.QueueStateEvent(walking,new KeyboardState());File.WriteAllText(Path.Combine(root,"entry-q-returned"),"PASS");}
                        }
                    }
                    var stealth=owner.GetComponent<EntrySkill_E>();float alpha=Field<float>(stealth,"_lastRemoteAlpha");
                    if(qReturned&&!stealth.IsStealth&&weaponTransforms==null){
                        var holder=Field<GameObject>(owner.GetComponent<PlayerWeaponViewController>(),"thirdPersonWeaponHolder");
                        weaponTransforms=holder.GetComponentsInChildren<Transform>(true);weaponPositions=weaponTransforms.Select(t=>t.localPosition).ToArray();weaponRotations=weaponTransforms.Select(t=>t.localRotation).ToArray();
                        EntryEvent("E-weapon-baseline","transforms="+weaponTransforms.Length);
                    }
                    if(!hidden&&stealth.IsStealth&&alpha<=.05f){
                        if(Field<Renderer[]>(stealth,"_remoteRenderers").Any(r=>r&&r.enabled)){Finish("FAILED: far enemy renderer visible");yield break;}
                        hidden=true;EntryEvent("E-hidden","distance="+Vector3.Distance(me.transform.position,owner.transform.position));File.WriteAllText(Path.Combine(root,"entry-hidden"),"PASS");}
                    if(hidden&&!near&&File.Exists(Path.Combine(root,"entry-approach"))){
                        if(Vector3.Distance(me.transform.position,owner.transform.position)>2.2f){motor.CanMove=true;InputSystem.QueueStateEvent(walking,new KeyboardState(Key.W));}
                        else {InputSystem.QueueStateEvent(walking,new KeyboardState());if(stealth.IsStealth&&alpha>=.999f){
                            var renderers=Field<Renderer[]>(stealth,"_remoteRenderers");var originals=Field<Material[][]>(stealth,"_remoteOriginalMaterials");
                            for(int i=0;i<renderers.Length;i++)if(renderers[i] is SkinnedMeshRenderer&&renderers[i].enabled&&!renderers[i].sharedMaterials.SequenceEqual(originals[i])){Finish("FAILED: near enemy original materials missing");yield break;}
                            if(weaponTransforms==null){Finish("FAILED: weapon baseline missing");yield break;}
                            for(int i=0;i<weaponTransforms.Length;i++)if(!weaponTransforms[i]||Vector3.Distance(weaponTransforms[i].localPosition,weaponPositions[i])>.00001f||Quaternion.Angle(weaponTransforms[i].localRotation,weaponRotations[i])>.01f){Finish("FAILED: stealth changed weapon mount");yield break;}
                            EntryEvent("E-original-materials","restored; weapon mount unchanged");
                            near=true;EntryEvent("E-near","alpha="+alpha);File.WriteAllText(Path.Combine(root,"entry-near"),"PASS");}}
                    }
                    int health=me.GetComponent<PlayerHP>().CurrentHP;
                    if(!hit&&health<hp){hit=true;EntryEvent("X-hit","hp="+health);File.WriteAllText(Path.Combine(root,"entry-hit"),health.ToString());hp=health;}
                    else if(hit&&!burst&&health<hp){burst=true;EntryEvent("X-burst-hit","hp="+health);File.WriteAllText(Path.Combine(root,"entry-burst-hit"),health.ToString());}
                }
                yield return null;
            }
            InputSystem.RemoveDevice(walking);
            if(!File.Exists(Path.Combine(root,"entry-finished"))){Finish("FAILED: Entry lesson did not finish");yield break;}
        }
        client.OnPacket-=ObserveEntryPacket;yield return new WaitForSecondsRealtime(.5f);
    }

    static bool EntryLaneClear(Vector3 origin,Vector3 direction,float distance) =>
        !Physics.CapsuleCastAll(origin+Vector3.up*.45f,origin+Vector3.up*1.5f,.32f,direction,distance,~0,QueryTriggerInteraction.Ignore)
            .Any(h=>!h.collider.GetComponentInParent<PlayerIdentity>());

    IEnumerator VerifyEntryIdle(PlayerIdentity actor,string view)
    {
        var movement=actor.GetComponent<PlayerMovement>();
        var animator=actor.transform.Find("Model").GetComponent<Animator>();
        var cc=actor.GetComponent<CharacterController>();
        yield return new WaitForSecondsRealtime(.5f);
        Vector3 start=actor.transform.position;float end=Time.realtimeSinceStartup+3;
        int frames=0;string clip="";
        while(Time.realtimeSinceStartup<end){
            yield return new WaitForEndOfFrame();
            int index=movement.GetCurrentAnimIndex();
            clip=string.Join("|",animator.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name));
            bool idleClip=clip.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0;
            if(index%6!=0||!idleClip||animator.IsInTransition(0)||
                (movement.IsMine&&!cc.isGrounded)||Vector3.Distance(start,actor.transform.position)>.08f||
                actor.GetComponent<EntryDashMotor>().IsDashing){
                Finish($"FAILED: {view} post-dash idle index={index} clip={clip} grounded={cc.isGrounded} position={actor.transform.position}");yield break;
            }
            frames++;
        }
        EntryEvent("C-idle-"+view,$"frames={frames}; clip={clip}; stable=3s");
    }

    IEnumerator VerifyEntryRemoteDash(PlayerIdentity owner,Vector3 direction)
    {
        yield return Wait(()=>File.Exists(Path.Combine(root,"entry-c-start")),"waiting for C continuity check",15);if(finished)yield break;
        Vector3 start=owner.transform.position,previous=start;
        float previousTime=Time.realtimeSinceStartup,maxStep=0,maxBacktrack=0,maxGap=0,lastMovement=-1;
        int movingFrames=0;float deadline=previousTime+4,settleUntil=-1;
        string endpoint=Path.Combine(root,"entry-c-endpoint.json");
        while(Time.realtimeSinceStartup<deadline){
            yield return new WaitForEndOfFrame();
            float now=Time.realtimeSinceStartup;Vector3 current=owner.transform.position,delta=current-previous;
            float along=Vector3.Dot(delta,direction),step=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;
            maxStep=Mathf.Max(maxStep,step);maxBacktrack=Mathf.Max(maxBacktrack,-along);
            if(step>.01f){
                movingFrames++;if(lastMovement>=0)maxGap=Mathf.Max(maxGap,now-lastMovement);lastMovement=now;
                if(step>28f*(now-previousTime)+.65f){Finish("FAILED: remote dash discontinuity="+step);yield break;}
            }
            previous=current;previousTime=now;
            if(File.Exists(endpoint)&&settleUntil<0)settleUntil=now+.3f;
            if(settleUntil>0&&now>=settleUntil)break;
        }
        if(!File.Exists(endpoint)){Finish("FAILED: no C endpoint");yield break;}
        var expected=JsonUtility.FromJson<EntryDashEndpoint>(File.ReadAllText(endpoint)).position;
        float error=Vector3.Distance(owner.transform.position,expected),travel=Vector3.Dot(owner.transform.position-start,direction);
        if(movingFrames<4||travel<3||maxBacktrack>.15f||maxGap>.16f||error>.2f){
            Finish($"FAILED: remote C frames={movingFrames} travel={travel:F3} backtrack={maxBacktrack:F3} gap={maxGap:F3} error={error:F3}");yield break;
        }
        EntryEvent("C-continuity",FormattableString.Invariant($"frames={movingFrames}; travel={travel:F3}; maxStep={maxStep:F3}; backtrack={maxBacktrack:F3}; gap={maxGap:F3}; endpointError={error:F3}"));
        File.WriteAllText(Path.Combine(root,"entry-c-remote-pass"),"PASS");
    }
}
#endif
