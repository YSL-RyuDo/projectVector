#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Renders saved production geometry/animation in an isolated scene. This is a
// visual review, deliberately not presented as a networked gameplay recording.
public static class SpikePresentationReview
{
    public static string Output="C:/Users/GCT/Documents/Vector-Spike-20261027/V2";
    public static void RevisionStills(){Output="C:/Users/GCT/Documents/Vector-Spike-20261027/V3";SpikePresentationAuthoring.Run();Stills();}
    public static void RevisionCapture(){Output="C:/Users/GCT/Documents/Vector-Spike-20261027/V3";CaptureAll();}
    public static void ControllerOnlyCapture(){Output="C:/Users/GCT/Documents/Vector-Spike-20261027/V4";CoatReview();}
    public static void CompleteInteractionCapture()
    {
        Output="C:/Users/GCT/Documents/Vector-Spike-20261027/V5";
        SpikePresentationAuthoring.Verify();
        Setup();
        var dropped=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Spike/DroppedSpike.prefab"));dropped.transform.position=origin;
        foreach(var b in dropped.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
        var idleVisual=dropped.GetComponentInChildren<SpikeDeviceVisual>();SpikeDetonation release=null;
        Capture("device-states",20,t=>{
            dropped.SetActive(t<3);if(release)release.gameObject.SetActive(t>=12&&t<16);
            if(t<3){World(45,0,t*12);device.SetActive(false);idleVisual.RenderDormant(t);}
            else if(t<9)World(45-(t-3),t-3,35+(t-3)*5);
            else if(t<12)World(3-(t-9),t-3,65);
            else if(t<16)
            {
                actor.SetActive(false);device.SetActive(false);
                if(!release){release=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SpikePresentationAuthoring.EffectPath)).GetComponent<SpikeDetonation>();release.transform.position=origin;release.enabled=false;}
                BlastCamera();release.Render(t-12);release.SampleParticles(t-12);
            }
            else{World(20,t,35);spike.pulseRing.gameObject.SetActive(false);spike.device.RenderSafe(t-16);}
            if(Mathf.Abs(t-1)<.01f)Still("device-spawn");
            if(Mathf.Abs(t-5)<.01f)Still("device-active");
            if(Mathf.Abs(t-18)<.01f)Still("device-disconnected");
        });
        CaptureInteractionViews();
        UnityEngine.Debug.Log("DEVICE_COMPLETE_CAPTURE_PASS: 20s states, 56s four third-person cycles with stand recovery, 14s one first-person cycle");
    }
    public static void RefreshCompleteViews(){Output="C:/Users/GCT/Documents/Vector-Spike-20261027/V5";CaptureInteractionViews();}
    static void CaptureInteractionViews()
    {
        Setup();int current=-1;
        Capture("all-characters",56,t=>{
            int index=Mathf.Min(3,(int)(t/14));float local=t-index*14;
            if(current!=index){SetActor(SpikePresentationAuthoring.Characters[index]);PrepareIdle(SpikePresentationAuthoring.Characters[index]);current=index;}
            CompleteInteractionFrame(false,local);
            camera.transform.position=actor.transform.position+Quaternion.Euler(0,Mathf.Lerp(30,100,local/14),0)*new Vector3(0,1.15f,2.45f);
            camera.transform.LookAt(actor.transform.position+Vector3.up*.80f);camera.fieldOfView=46;
            if(index==1&&(local>2.15f&&local<3.75f||local>6.4f&&local<10.5f))VerifyCoatShoes();
            if(Mathf.Abs(local-3.2f)<.01f)Still(SpikePresentationAuthoring.Characters[index]+"-plant");
            if(Mathf.Abs(local-8)<.01f)Still(SpikePresentationAuthoring.Characters[index]+"-defuse");
            if(Mathf.Abs(local-10.8f)<.01f)Still(SpikePresentationAuthoring.Characters[index]+"-rising");
            if(Mathf.Abs(local-12)<.01f)Still(SpikePresentationAuthoring.Characters[index]+"-standing");
        });
        Setup();SetActor("Controller");PrepareIdle("Controller");
        Capture("one-first-person",14,t=>CompleteInteractionFrame(true,t));
        UnityEngine.Debug.Log("DEVICE_INTERACTION_VIEWS_PASS: full-body framing, four third-person cycles, one first-person cycle");
    }
    static void PrepareIdle(string character)
    {
        string path=character=="Controller"?"Assets/Character(New)/Controller/Animation/InGame/LongGun/Controller_LongGun_Idle.anim":"Assets/Character Animation/"+character+"/InGame/"+(character=="Initiator"?"Long Gun":"LongGun")+"/"+character+"_LongGun_Idle.anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip)throw new Exception("Missing review idle: "+path);
        var model=actor.transform.Find("Model");var position=model.localPosition;var rotation=model.localRotation;
        clip.SampleAnimation(model.gameObject,0);model.localPosition=position;model.localRotation=rotation;
    }
    static void CompleteInteractionFrame(bool fp,float t)
    {
        if(t<.75f){Frame(fp,0,0);device.SetActive(false);}
        else if(t<3.75f)Frame(fp,1,t-.75f);
        else if(t<5.5f)
        {
            Frame(fp,0,0);if(!fp)SpikeInteractionPresentation.SampleRecovery(pose.thirdPerson,t-3.75f);
            device.SetActive(true);spike.Render(45-(t-3.75f),t-3.75f,Mathf.Repeat(t-3.75f,1));
        }
        else if(t<10.5f)Frame(fp,2,t-5.5f);
        else
        {
            Frame(fp,0,0);if(!fp)SpikeInteractionPresentation.SampleRecovery(pose.thirdPerson,t-10.5f);
            device.SetActive(true);spike.pulseRing.gameObject.SetActive(false);spike.device.RenderSafe(t-10.5f);
        }
        if(!fp)actor.GetComponent<ControllerCoatMotion>()?.Step(1f/30);
        if(fp)
        {
            // The isolated review has no equipped firearm animator. Hide its
            // neutral arm mesh between interactions instead of showing a T-pose.
            bool interacting=t>=.75f&&t<3.75f||t>=5.5f&&t<10.5f;
            foreach(var renderer in actor.transform.Find("CameraRoot/Controller Model_Riging (1)").GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if(renderer.name=="Arm1"||renderer.name=="Arm2")renderer.enabled=interacting;
        }
        if(fp&&(t>=3.75f&&t<5.5f||t>=10.5f))
        {
            actor.transform.Find("CameraRoot").localRotation=viewRootRotation*Quaternion.Euler(55,0,0);
            var original=actor.GetComponentInChildren<Camera>(true);camera.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);
        }
    }
    public static void GripStills()
    {
        Output="C:/Users/GCT/Documents/Vector-Spike-20261027/V4";SpikePresentationAuthoring.Run();Stills();
        SetActor("Controller");Frame(false,2,2);
        for(int i=0;i<60;i++)actor.GetComponent<ControllerCoatMotion>().Step(1f/60);
        VerifyCoatShoes();UnityEngine.Debug.Log("COAT_CONTACT_PASS: settled defuse pose");
    }
    public static void CoatAudit()
    {
        Output="C:/Users/GCT/Documents/Vector-Spike-20261027/V4";Setup();SetActor("Controller");
        Frame(false,2,2);var cloth=actor.GetComponent<ControllerCoatMotion>();
        for(int i=0;i<60;i++)cloth.Step(1f/60);
        camera.transform.position=actor.transform.position+new Vector3(2,1.05f,0);camera.transform.LookAt(actor.transform.position+Vector3.up*.55f);
        var lines=new System.Collections.Generic.List<string>();
        foreach(var b in cloth.legJoints)lines.Add(b.name+"="+actor.transform.InverseTransformPoint(b.position).ToString("F4"));
        for(int c=0;c<cloth.columns;c+=3)for(int r=0;r<cloth.rows;r++)
        {int i=c*cloth.rows+r;lines.Add(cloth.nodes[i].name+"="+actor.transform.InverseTransformPoint(cloth.nodes[i].position).ToString("F4")+" rest="+cloth.restPoints[i].ToString("F4"));}
        File.WriteAllLines(Output+"/coat-nodes.txt",lines);
        var renderer=actor.transform.Find("Model").GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="HeadUV");
        renderer.sharedMesh=AssetDatabase.LoadAllAssetsAtPath("Assets/Character(New)/Controller/Controller Model_Riging (1).fbx").OfType<Mesh>().First(m=>m.name=="HeadUV");
        Still("coat-original-kneel");
        SpikeInteractionPresentation.Sample(pose.thirdPerson,0,0);Still("coat-original-stand");
        UnityEngine.Debug.Log("COAT_SCALE "+actor.transform.lossyScale+" "+string.Join(";",renderer.bones.Where(b=>b.name.EndsWith("Hips")||b.name.EndsWith("Spine")).Select(b=>b.name+"="+(b.position-actor.transform.position))));
        var baked=new Mesh();renderer.BakeMesh(baked);var vtx=baked.vertices.Select(p=>renderer.transform.TransformPoint(p)-actor.transform.position).ToArray();
        var mesh=renderer.sharedMesh;var vv=mesh.vertices;var ww=mesh.boneWeights;var mm=renderer.bones.Take(mesh.bindposes.Length).Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
        var cpu=vv.Select((v,i)=>{var w=ww[i];return mm[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+mm[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+mm[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+mm[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3-actor.transform.position;}).ToArray();
        UnityEngine.Debug.Log("COAT_BAKE_SCALE renderer="+renderer.transform.lossyScale+" cpuY="+cpu.Max(v=>v.y)+" bakeY="+vtx.Max(v=>v.y));
        foreach(float height in new[]{.8f,.9f,1f,1.1f,1.2f})ControllerCoatAuthoring.AuditIslands(cpu,mesh.triangles,height);
    }
    public static void GripCapture()
    {
        Output="C:/Users/GCT/Documents/Vector-Spike-20261027/V4";
        SpikePresentationAuthoring.Run();Stills();
        Setup();SetActor("Controller");
        Capture("shared-first-person",9,t=>Frame(true,t<3.5f?(byte)1:(byte)2,t<3.5f?Mathf.Min(t,3):Mathf.Min(t-3.5f,5)));
        Setup();int current=-1;
        Capture("four-third-person",36,t=>{
            int index=Mathf.Min(3,(int)(t/9));float local=t-index*9;
            if(current!=index){SetActor(SpikePresentationAuthoring.Characters[index]);current=index;}
            Frame(false,local<3.5f?(byte)1:(byte)2,local<3.5f?Mathf.Min(local,3):Mathf.Min(local-3.5f,5));
            if(index==1 && (local>1.4f&&local<3.5f||local>4.4f))VerifyCoatShoes();
            camera.transform.position=actor.transform.position+new Vector3(1.2f,1.05f,1.5f);
            camera.transform.LookAt(actor.transform.position+Vector3.up*.65f+Vector3.forward*.15f);camera.fieldOfView=46;
        });
        Setup();SetActor("Controller");
        Capture("coat-foot-close",12,t=>{
            Frame(false,2,Mathf.Min(t,5));
            camera.transform.position=actor.transform.position+Quaternion.Euler(0,Mathf.Lerp(-40,110,t/12),0)*new Vector3(.9f,.48f,-1.2f);
            camera.transform.LookAt(actor.transform.position+Vector3.up*.32f);camera.fieldOfView=38;
            if(t>.8f)VerifyCoatShoes();
        });
        CoatReview();
    }
    static void VerifyCoatShoes()
    {
        var cloth=actor.GetComponent<ControllerCoatMotion>();if(!cloth)return;
        var renderer=actor.transform.Find("Model").GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="HeadUV");
        var mesh=renderer.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
        var matrices=renderer.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
        for(int i=0;i<vertices.Length;i++)
        {
            var w=weights[i];if(!renderer.bones[w.boneIndex0].name.StartsWith("Coat_"))continue;
            var v=vertices[i];var world=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
            foreach(var shoe in cloth.shoes)
            {
                var a=shoe.bone.TransformPoint(shoe.heel);var b=shoe.bone.TransformPoint(shoe.toe);var ab=b-a;
                var nearest=a+ab*Mathf.Clamp01(Vector3.Dot(world-a,ab)/Mathf.Max(.00001f,ab.sqrMagnitude));
                float distance=Vector3.Distance(world,nearest);
                if(distance<shoe.radius-.04f)throw new Exception("Coat intersects shoe volume: "+(shoe.radius-.035f-distance).ToString("F3")+"m");
            }
            for(int leg=0;leg<2;leg++)
            {
                var upper=cloth.legJoints[leg*3].position;var knee=cloth.legJoints[leg*3+1].position;var thigh=knee-upper;
                var onThigh=upper+thigh*Mathf.Clamp01(Vector3.Dot(world-upper,thigh)/Mathf.Max(.00001f,thigh.sqrMagnitude));
                if(Vector3.Distance(world,onThigh)<.065f)throw new Exception("Coat intersects thigh: vertex="+i+" distance="+Vector3.Distance(world,onThigh).ToString("F4"));
                var a=cloth.legJoints[leg*3+1].position;var b=cloth.legJoints[leg*3+2].position;var ab=b-a;
                var nearest=a+ab*Mathf.Clamp01(Vector3.Dot(world-a,ab)/Mathf.Max(.00001f,ab.sqrMagnitude));
                if(Vector3.Distance(world,nearest)<.055f)
                {
                    float closest=float.MaxValue;
                    foreach(var s in cloth.surface){Vector3 Point(int id,Vector3 offset)=>cloth.nodes[id].position+cloth.nodes[id].rotation*offset;var q=Point(s.a,s.oa)*s.weights.x+Point(s.b,s.ob)*s.weights.y+Point(s.c,s.oc)*s.weights.z+Point(s.d,s.od)*s.weights.w;closest=Mathf.Min(closest,Vector3.Distance(q,world));}
                    throw new Exception("Coat intersects lower leg, vertex="+i+" distance="+Vector3.Distance(world,nearest).ToString("F4")+" sample mismatch="+closest.ToString("F4")+" point="+world+" leg="+leg+" bones="+renderer.bones[w.boneIndex0].name+","+renderer.bones[w.boneIndex1].name+","+renderer.bones[w.boneIndex2].name+","+renderer.bones[w.boneIndex3].name+" weights="+new Vector4(w.weight0,w.weight1,w.weight2,w.weight3)+" anchor="+cloth.anchors[0].Position()+" hips="+cloth.anchors[0].bones[0].position);
                }
            }
        }
    }
    const int W=1280,H=720;
    static Camera camera;
    static RenderTexture target;
    static Texture2D pixels;
    static GameObject actor, device;
    static SpikePresentation spike;
    static SpikeInteractionPresentation pose;
    static Vector3 origin;
    static Quaternion viewRootRotation;
    static bool settleStills;
    static void Setup()
    {
        Directory.CreateDirectory(Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/TestMap.prefab"));
        foreach(var behaviour in map.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.64f,.72f,.82f);RenderSettings.ambientEquatorColor=new Color(.42f,.48f,.55f);RenderSettings.ambientGroundColor=new Color(.22f,.25f,.28f);RenderSettings.fog=false;
        var sun=new GameObject("Review sunlight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.transform.rotation=Quaternion.Euler(45,-30,0);sun.shadows=LightShadows.Soft;
        Physics.SyncTransforms();
        origin=FindStage();
        camera=new GameObject("Review camera").AddComponent<Camera>();camera.nearClipPlane=.02f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.31f,.42f,.56f);camera.fieldOfView=70;
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        target=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);target.antiAliasing=4;target.Create();camera.targetTexture=target;
        pixels=new Texture2D(W,H,TextureFormat.RGB24,false);
        device=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Spike/Spike.prefab"));device.transform.position=origin;
        foreach(var b in device.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
        spike=device.GetComponent<SpikePresentation>();
        SetActor("Initiator");
    }
    static Vector3 FindStage()
    {
        bool Floor(Vector3 p,out RaycastHit hit)=>Physics.Raycast(new Vector3(p.x,20,p.z),Vector3.down,out hit,40,~0,QueryTriggerInteraction.Ignore)&&hit.normal.y>.95f;
        var seed=new Vector3(20,0,-37);
        for(int radius=0;radius<12;radius++)for(int k=0;k<Mathf.Max(1,radius*8);k++)
        {
            var p=seed+Quaternion.Euler(0,k*360f/Mathf.Max(1,radius*8),0)*Vector3.forward*(radius*2);
            if(!Floor(p,out var hit)||hit.point.y>3)continue;
            bool flat=true;
            for(int i=0;i<48;i++)
            {
                var q=p+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*(1+i/16);
                if(!Floor(q,out var adjacent)||Mathf.Abs(adjacent.point.y-hit.point.y)>.03f){flat=false;break;}
            }
            if(flat){UnityEngine.Debug.Log("DEVICE_REVIEW_STAGE "+hit.point);return hit.point+Vector3.up*.015f;}
        }
        throw new Exception("No unobstructed flat review area found in TestMap");
    }
    static void SetActor(string character)
    {
        if(actor)Object.DestroyImmediate(actor);
        actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/"+character+"/"+character+".prefab"));
        actor.transform.position=origin-Vector3.forward*PlayerSpike.PlantForwardOffset;
        viewRootRotation=actor.transform.Find("CameraRoot").localRotation;
        foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
        foreach(var c in actor.GetComponentsInChildren<Camera>(true))c.enabled=false;
        foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
        foreach(var p in actor.GetComponentsInChildren<ParticleSystem>(true))p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach(var r in actor.GetComponentsInChildren<LineRenderer>(true))r.enabled=false;
        var rootRenderer=actor.GetComponent<Renderer>();if(rootRenderer)rootRenderer.enabled=false;
        // Use the production view's body/arm partition, no substitute arm mesh.
        var fp=actor.transform.Find("CameraRoot/Controller Model_Riging (1)");
        fp.gameObject.SetActive(true);
        foreach(var r in fp.GetComponentsInChildren<Renderer>(true))
            if(r is SkinnedMeshRenderer)r.enabled=r.name=="Arm1"||r.name=="Arm2";
            else if(!r.transform.IsChildOf(actor.GetComponent<SpikeInteractionPresentation>().firstPerson.device.transform)&&!r.transform.IsChildOf(actor.GetComponent<SpikeInteractionPresentation>().firstPerson.tool.transform))r.enabled=false;
        actor.GetComponent<FirstPersonViewSetup>()?.Setup(true);
        pose=actor.GetComponent<SpikeInteractionPresentation>();
        // Keep the production actor origin on the floor. Foot bone pivots lie above
        // the soles and must not be used to push the whole character down for review.
        // Force skin matrices after each sampled pose in this isolated edit-mode renderer.
        foreach(var r in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {r.updateWhenOffscreen=true;r.forceMatrixRecalculationPerRender=true;}
    }
    static void Frame(bool fp,byte action,float time)
    {
        actor.SetActive(true);
        actor.transform.Find("Model").gameObject.SetActive(!fp);
        actor.transform.Find("CameraRoot").gameObject.SetActive(fp);
        actor.transform.Find("CameraRoot").localRotation=viewRootRotation*(fp?Quaternion.Euler(action==2?42:Mathf.Lerp(0,16,Mathf.Clamp01((time-2)/1)),0,0):Quaternion.identity);
        camera.cullingMask=~0;
        if(fp)
        {
            camera.cullingMask &= ~(1 << LayerMask.NameToLayer("PlayerBody"));
            var original=actor.GetComponentInChildren<Camera>(true);camera.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);camera.fieldOfView=80;
            SpikeInteractionPresentation.Sample(pose.firstPerson,action,time);SpikeInteractionPresentation.Sample(pose.thirdPerson,0,0);
        }
        else
        {
            camera.transform.position=actor.transform.position+new Vector3(1.8f,1.25f,2.2f);camera.transform.LookAt(actor.transform.position+Vector3.up*.7f);camera.fieldOfView=48;
            SpikeInteractionPresentation.Sample(pose.thirdPerson,action,time);SpikeInteractionPresentation.Sample(pose.firstPerson,0,0);
        }
        device.SetActive(action==2);
        if(action==2)spike.Render(25,5,Mathf.Repeat(time,1));
        if(!fp)actor.GetComponent<ControllerCoatMotion>()?.Step(1f/30);
    }
    static byte[] Render()
    {
        camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,W,H),0,0);pixels.Apply();RenderTexture.active=null;return pixels.GetRawTextureData();
    }
    static void BlastCamera()
    {
        camera.fieldOfView=65;
        var aim=origin+Vector3.up*.6f;
        Physics.SyncTransforms();
        for(int i=0;i<12;i++)
        {
            var p=origin+Quaternion.Euler(0,i*30,0)*new Vector3(0,2.1f,-5.8f);
            if(!Physics.Linecast(p,aim,~0,QueryTriggerInteraction.Ignore)){camera.transform.position=p;camera.transform.LookAt(aim);return;}
        }
        camera.transform.position=origin+new Vector3(0,2.1f,-5.8f);camera.transform.LookAt(aim);
    }
    static void Still(string name)
    {
        if(settleStills&&actor&&actor.activeSelf&&actor.transform.Find("Model").gameObject.activeSelf)
            for(int i=0;i<50;i++)actor.GetComponent<ControllerCoatMotion>()?.Step(1f/60);
        Render();File.WriteAllBytes(Output+"/"+name+".png",pixels.EncodeToPNG());
    }
    static void World(float remaining,float age,float orbit=0)
    {
        actor.SetActive(false);device.SetActive(true);camera.cullingMask=~0;camera.fieldOfView=43;
        camera.transform.position=origin+Quaternion.Euler(0,orbit,0)*new Vector3(.60f,.46f,-.88f);camera.transform.LookAt(origin+Vector3.up*.20f);
        spike.Render(remaining,age,Mathf.Repeat(age,SpikePresentation.BeatInterval(remaining)));
    }
    public static void Stills()
    {
        settleStills=true;
        Setup();
        foreach(string character in SpikePresentationAuthoring.Characters)
        {
            SetActor(character);Frame(true,1,1.2f);Still(character+"-plant");Frame(true,2,1.2f);Still(character+"-defuse");Frame(false,1,1.2f);Still(character+"-world");
            if(character=="Controller"){Frame(true,1,2.77f);Still("shared-first-person-contact");}
            Frame(false,1,2.9f);Still(character+"-place");
            Frame(false,2,2f);Still(character+"-world-defuse");
            camera.transform.position=actor.transform.position+new Vector3(2,1.05f,0);camera.transform.LookAt(actor.transform.position+Vector3.up*.55f);Still(character+"-side");
            camera.transform.position=actor.transform.position+new Vector3(.9f,.85f,-1.9f);camera.transform.LookAt(actor.transform.position+Vector3.up*.5f);Still(character+"-back");
            if(character=="Entry")foreach(var b in pose.thirdPerson.bones.Where(b=>b.name=="Hips"||b.name=="Spine"||b.name=="LeftUpLeg"||b.name=="LeftLeg"||b.name=="LeftFoot"))UnityEngine.Debug.Log("ENTRY_REVIEW_BONE "+b.name+" "+(b.position-origin).ToString("F3"));
        }
        World(30,3,0);Still("device-armed");World(3,3,30);Still("device-critical");
        World(45,0,0);spike.pulseRing.gameObject.SetActive(false);spike.device.RenderDormant(1);Still("device-dormant");
        spike.device.RenderSafe(1.5f);Still("device-safe");
        actor.SetActive(false);device.SetActive(false);
        var explosion=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SpikePresentationAuthoring.EffectPath)).GetComponent<SpikeDetonation>();explosion.transform.position=origin;explosion.enabled=false;
        BlastCamera();
        foreach(float time in new[]{.13f,.35f,1f,2f}){explosion.Render(time);explosion.SampleParticles(time);Still("blast-"+time.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));}
        UnityEngine.Debug.Log("SPIKE_STILLS_PASS "+Output);
        settleStills=false;
    }
    public static void Video()
    {
        Setup();
        string ffmpeg="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
        var info=new ProcessStartInfo(ffmpeg,"-y -loglevel error -f rawvideo -pix_fmt rgb24 -s "+W+"x"+H+" -r 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p \""+Output+"/Spike-visual-review-silent.mp4\"") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
        using var process=Process.Start(info);
        var errors=process.StandardError.ReadToEndAsync();
        const int frames=31*30;
        SpikeDetonation explosion=null;
        for(int frame=0;frame<frames;frame++)
        {
            float t=frame/30f;
            if(t<3)Frame(true,1,t);
            else if(t<6)Frame(false,1,t-3);
            else if(t<11)World(45-(t-6),t-6,(t-6)*15);
            else if(t<16)Frame(true,2,t-11);
            else if(t<18){World(20,t,25);spike.pulseRing.gameObject.SetActive(false);spike.device.RenderSafe(t-16);}
            else if(t<27){float remain=t<21?19-(t-18):t<24?9-(t-21):4-(t-24);World(remain,t,25);}
            else
            {
                actor.SetActive(false);device.SetActive(false);
                if(!explosion){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SpikePresentationAuthoring.EffectPath));explosion=g.GetComponent<SpikeDetonation>();g.transform.position=origin;explosion.enabled=false;}
                BlastCamera();
                explosion.Render(t-27);explosion.SampleParticles(t-27);
            }
            byte[] bytes=Render();process.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
        }
        process.StandardInput.Close();process.WaitForExit();if(process.ExitCode!=0)throw new Exception(errors.Result);
        UnityEngine.Debug.Log("SPIKE_VIDEO_PASS");
    }
    static void Capture(string name,int seconds,Action<float> frameAction)
    {
        string ffmpeg="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
        var info=new ProcessStartInfo(ffmpeg,"-y -loglevel error -f rawvideo -pix_fmt rgb24 -s "+W+"x"+H+" -r 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p \""+Output+"/"+name+"-silent.mp4\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
        using var process=Process.Start(info);var errors=process.StandardError.ReadToEndAsync();
        for(int f=0;f<seconds*30;f++)
        {
            frameAction(f/30f);var bytes=Render();process.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            if(f%270==0)UnityEngine.Debug.Log("DEVICE_CAPTURE "+name+" "+f/30+"s");
        }
        process.StandardInput.Close();process.WaitForExit();if(process.ExitCode!=0)throw new Exception(errors.Result);
        UnityEngine.Debug.Log("DEVICE_CAPTURE_PASS "+name);
    }
    public static void CaptureAll()
    {
        SpikePresentationAuthoring.Run();Stills();
        foreach(bool fp in new[]{false,true})
        {
            Setup();int current=-1;
            Capture(fp?"characters-first-person":"characters-third-person",36,t=>{
                int index=Mathf.Min(3,(int)(t/9));float local=t-index*9;
                if(index!=current){SetActor(SpikePresentationAuthoring.Characters[index]);current=index;}
                bool planting=local<3.5f;float elapsed=planting?Mathf.Min(3,local):Mathf.Min(5,local-3.5f);
                Frame(fp,planting?(byte)1:(byte)2,elapsed);
            });
        }
        Setup();
        var dropped=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Spike/DroppedSpike.prefab"));dropped.transform.position=origin;
        foreach(var b in dropped.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
        var idleVisual=dropped.GetComponentInChildren<SpikeDeviceVisual>();
        SpikeDetonation release=null;
        Capture("device-lifecycle",25,t=>{
            dropped.SetActive(t<3);
            if(release)release.gameObject.SetActive(t>=13&&t<17);
            if(t<3){World(45,0,t*18);device.SetActive(false);idleVisual.RenderDormant(t);}
            else if(t<6)Frame(true,1,t-3);
            else if(t<10)World(45-(t-6),t-6,15);
            else if(t<13)World(3-(t-10),t,15);
            else if(t<17)
            {
                actor.SetActive(false);device.SetActive(false);
                if(!release){release=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SpikePresentationAuthoring.EffectPath)).GetComponent<SpikeDetonation>();release.transform.position=origin;release.enabled=false;}
                BlastCamera();release.Render(t-13);release.SampleParticles(t-13);
            }
            else if(t<22)Frame(true,2,t-17);
            else{World(20,t,15);spike.pulseRing.gameObject.SetActive(false);spike.device.RenderSafe(t-22);}
            if(Mathf.Abs(t-1)<.01f)Still("spawn");
            if(Mathf.Abs(t-22.8f)<.01f)Still("disconnected");
        });
        UnityEngine.Debug.Log("DEVICE_ALL_MEDIA_PASS "+Output);
        CoatReview();
    }
    public static void RefreshThirdPerson()
    {
        SpikePresentationAuthoring.Run();Stills();Setup();int current=-1;
        Capture("characters-third-person",36,t=>{
            int index=Mathf.Min(3,(int)(t/9));float local=t-index*9;
            if(index!=current){SetActor(SpikePresentationAuthoring.Characters[index]);current=index;}
            bool planting=local<3.5f;
            Frame(false,planting?(byte)1:(byte)2,planting?Mathf.Min(3,local):Mathf.Min(5,local-3.5f));
        });
    }
    public static void CoatReview()
    {
        Setup();SetActor("Controller");device.SetActive(false);
        var model=actor.transform.Find("Model");model.gameObject.SetActive(true);actor.transform.Find("CameraRoot").gameObject.SetActive(false);
        var originalPosition=model.localPosition;var originalRotation=model.localRotation;
        string folder="Assets/Character(New)/Controller/Animation/InGame/LongGun/";
        AnimationClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Controller_LongGun_"+name+".anim");
        var idle=Clip("Idle");var walk=Clip("Walk");var crouch=Clip("Crouch_Idle");var crouchWalk=Clip("Crouch_Walk");
        if(!idle||!walk||!crouch||!crouchWalk)throw new Exception("Missing controller locomotion clips");
        var cloth=actor.GetComponent<ControllerCoatMotion>();cloth.ResetMotion();
        var bodyBones=model.GetComponentsInChildren<Transform>(true).Where(b=>b!=model&&!b.name.StartsWith("Coat")&&!b.IsChildOf(model.Find("Coat dynamics"))).ToArray();
        var blendPositions=new Vector3[bodyBones.Length];var blendRotations=new Quaternion[bodyBones.Length];
        Capture("controller-coat-motion",20,t=>{
            var clip=t<2?idle:t<7?walk:t<10?crouch:t<15?crouchWalk:idle;
            model.localPosition=originalPosition;model.localRotation=originalRotation;
            float transition=t<2?0:t<7?2:t<10?7:t<15?10:15;
            var previousClip=t<2?idle:t<7?idle:t<10?walk:t<15?crouch:crouchWalk;
            float blend=transition==0?1:Mathf.SmoothStep(0,1,(t-transition)/.4f);
            if(blend<1)
            {
                previousClip.SampleAnimation(model.gameObject,Mathf.Repeat(t,previousClip.length));
                for(int i=0;i<bodyBones.Length;i++){blendPositions[i]=bodyBones[i].localPosition;blendRotations[i]=bodyBones[i].localRotation;}
            }
            clip.SampleAnimation(model.gameObject,Mathf.Repeat(t,clip.length));
            if(blend<1)for(int i=0;i<bodyBones.Length;i++){bodyBones[i].localPosition=Vector3.Lerp(blendPositions[i],bodyBones[i].localPosition,blend);bodyBones[i].localRotation=Quaternion.Slerp(blendRotations[i],bodyBones[i].localRotation,blend);}
            model.localPosition=originalPosition;model.localRotation=originalRotation;
            float move=t<2?0:t<7?Mathf.Sin((t-2)*Mathf.PI/5)*.6f:t<10?0:t<15?Mathf.Sin((t-10)*Mathf.PI/5)*.4f:0;
            float yaw=t<7?0:t<10?Mathf.SmoothStep(0,75,(t-7)/3):75;
            actor.transform.SetPositionAndRotation(origin+new Vector3(0,0,move-.5f),Quaternion.Euler(0,yaw,0));
            cloth.Step(1f/30);
            for(int i=0;i<cloth.nodes.Length;i++)
                if(float.IsNaN(cloth.nodes[i].position.x)||Vector3.Distance(cloth.nodes[i].position,actor.transform.position)>3)throw new Exception("Unstable coat simulation");
            camera.cullingMask=~0;camera.fieldOfView=42;
            camera.transform.position=actor.transform.position+actor.transform.rotation*Quaternion.Euler(0,35+t*16,0)*new Vector3(0,1.05f,2.5f);
            camera.transform.LookAt(actor.transform.position+Vector3.up*.75f);
            if(Mathf.Abs(t-4)<.01f)Still("controller-coat-walk");
            if(Mathf.Abs(t-9)<.01f)Still("controller-coat-crouch");
            if(Mathf.Abs(t-1)<.01f)Still("controller-coat-standing");
            if(Mathf.Abs(t-17)<.01f)Still("controller-coat-restored");
        });
        UnityEngine.Debug.Log("COAT_MOTION_PASS: 600 locomotion/crouch/turn frames, finite constrained secondary motion");
    }
}
#endif
