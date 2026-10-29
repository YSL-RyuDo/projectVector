#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Edit-time authoring only. Everything is saved in inspectable prefab hierarchies.
public static class ScannerPolishAuthoring
{
    const string Dir="Assets/Art/Scanner";
    const string ActorPath="Assets/Prefab/Character/Initiator/Initiator.prefab";
    const string ProjectilePath="Assets/Prefab/Character/Initiator/Scanner.prefab";
    public const string Output="C:/Users/GCT/Downloads/Vector_Scanner_20261022";
    static readonly Color Cyan=new Color(.14f,.74f,.88f);
    static void Ref(Object target,string field,Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Refs(Object target,string field,IEnumerable<Object> values){var a=values.ToArray();var s=new SerializedObject(target);var p=s.FindProperty(field);p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];s.ApplyModifiedPropertiesWithoutUndo();}
    static T Component<T>(GameObject g)where T:Component{var c=g.GetComponent<T>();return c?c:g.AddComponent<T>();}
    static Transform Child(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
    static void RemoveChild(Transform p,string name){var t=p.Find(name);if(t)Object.DestroyImmediate(t.gameObject);}
    static Material Mat(string name,Color color,float metal,bool glow=false)
    {
        string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",.5f);
        if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.8f);}
        EditorUtility.SetDirty(m);return m;
    }
    static Mesh Mesh(string name,List<Vector3> vertices,List<int> indices)
    {
        for(int i=0;i<indices.Count;i+=3){int swap=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=swap;}
        var flat=indices.Select(i=>vertices[i]).ToArray();var mesh=new Mesh{name=name};mesh.vertices=flat;mesh.triangles=Enumerable.Range(0,flat.Length).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Dir+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh Housing()
    {
        var v=new List<Vector3>();var t=new List<int>();const int n=12;
        float[] r={.083f,.112f,.112f,.086f},y={-.055f,-.03f,.035f,.065f};
        for(int j=0;j<4;j++)for(int i=0;i<n;i++){float a=(i+.5f)*2*Mathf.PI/n;v.Add(new Vector3(r[j]*Mathf.Cos(a),y[j],r[j]*Mathf.Sin(a)));}
        for(int j=0;j<3;j++)for(int i=0;i<n;i++){int a=j*n+i,b=j*n+(i+1)%n;t.AddRange(new[]{a,b,a+n,b,b+n,a+n});}
        for(int i=1;i<n-1;i++){t.AddRange(new[]{0,i+1,i});t.AddRange(new[]{3*n,3*n+i,3*n+i+1});}
        return Mesh("BeveledSensorCore",v,t);
    }
    static Mesh Petal()
    {
        Vector2[] outline={new Vector2(-.035f,.08f),new Vector2(-.044f,.19f),new Vector2(-.025f,.245f),new Vector2(.025f,.245f),new Vector2(.044f,.19f),new Vector2(.035f,.08f)};
        var v=new List<Vector3>();var t=new List<int>();
        for(int j=0;j<2;j++)foreach(var p in outline)v.Add(new Vector3(p.x,j==0?-.012f:.012f,p.y));
        for(int i=0;i<6;i++){int b=(i+1)%6;t.AddRange(new[]{i,i+6,b,b,i+6,b+6});}
        for(int i=1;i<5;i++){t.AddRange(new[]{0,i,i+1});t.AddRange(new[]{6,6+i+1,6+i});}
        return Mesh("TaperedSensorPetal",v,t);
    }
    static Transform Part(Transform p,string name,Mesh mesh,Material mat,Vector3 pos,Vector3 scale)
    {var t=Child(p,name);t.localPosition=pos;t.localScale=scale;Component<MeshFilter>(t.gameObject).sharedMesh=mesh;Component<MeshRenderer>(t.gameObject).sharedMaterial=mat;return t;}
    static void Box(Transform p,string name,Material mat,Vector3 pos,Vector3 size)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;}
    static void Device()
    {
        var black=Mat("Graphite",new Color(.045f,.06f,.075f),.65f);var titanium=Mat("Titanium",new Color(.34f,.41f,.44f),.8f);
        var cyan=Mat("SensorGlass",Cyan,.4f,true);var amber=Mat("WarningAmber",new Color(.92f,.43f,.08f),.35f);
        var core=Housing();var petal=Petal();var root=new GameObject("ScannerDevice");
        try{
            Part(root.transform,"ArmoredCore",core,black,Vector3.zero,Vector3.one);
            Part(root.transform,"TopCollar",core,titanium,Vector3.up*.055f,new Vector3(.72f,.25f,.72f));
            Part(root.transform,"SensorLens",core,cyan,Vector3.up*.075f,new Vector3(.45f,.08f,.45f));
            Part(root.transform,"LowerCollar",core,titanium,Vector3.down*.04f,new Vector3(.9f,.16f,.9f));
            for(int i=0;i<3;i++){
                var pivot=Child(root.transform,"Petal_"+i);pivot.localRotation=Quaternion.Euler(0,i*120,0)*Quaternion.Euler(-65,0,0);
                Part(pivot,"FacetedArmor",petal,black,Vector3.zero,Vector3.one);
                Part(pivot,"InsetPanel",petal,titanium,Vector3.up*.014f,new Vector3(.74f,.2f,.91f));
                Box(pivot,"SignalStrip",cyan,new Vector3(0,.022f,.173f),new Vector3(.012f,.005f,.09f));
                Box(pivot,"WarningTip",amber,new Vector3(0,.023f,.221f),new Vector3(.041f,.006f,.011f));
                Box(pivot,"Hinge",titanium,new Vector3(0,0,.082f),new Vector3(.077f,.04f,.025f));
            }
            PrefabUtility.SaveAsPrefabAsset(root,Dir+"/ScannerDevice.prefab");
        }finally{Object.DestroyImmediate(root);}
    }
    static LineRenderer Line(Transform p,string name)
    {
        var l=Child(p,name).gameObject.AddComponent<LineRenderer>();l.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone/EnergyBeam.mat");
        l.useWorldSpace=true;l.positionCount=0;l.enabled=false;l.widthCurve=AnimationCurve.Constant(0,1,1);l.widthMultiplier=.014f;l.numCapVertices=2;l.numCornerVertices=2;
        l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;return l;
    }
    static ParticleSystem Sparks(Transform p,string name,int count)
    {
        var ps=Child(p,name).gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=ps.main;m.playOnAwake=false;m.loop=false;m.duration=.5f;m.startLifetime=new ParticleSystem.MinMaxCurve(.1f,.3f);m.startSpeed=new ParticleSystem.MinMaxCurve(.8f,3.5f);m.startSize=new ParticleSystem.MinMaxCurve(.008f,.022f);m.startColor=Cyan;m.simulationSpace=ParticleSystemSimulationSpace.World;m.maxParticles=48;
        var e=ps.emission;e.rateOverTime=0;e.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});var s=ps.shape;s.shapeType=ParticleSystemShapeType.Sphere;s.radius=.05f;
        var colors=ps.colorOverLifetime;colors.enabled=true;var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(Cyan,0),new GradientColorKey(Cyan,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});colors.color=grad;
        var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone/EnergyParticle.mat");r.renderMode=ParticleSystemRenderMode.Stretch;r.lengthScale=2;r.velocityScale=.03f;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return ps;
    }
    static void Projectile()
    {
        var root=PrefabUtility.LoadPrefabContents(ProjectilePath);
        try{
            RemoveChild(root.transform,"Cube");RemoveChild(root.transform,"Presentation");
            var legacy=root.GetComponent<LineRenderer>();if(legacy){legacy.enabled=false;legacy.positionCount=0;legacy.widthMultiplier=.01f;}
            var p=Child(root.transform,"Presentation");var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/ScannerDevice.prefab"),p);
            var fx=Component<ScannerPresentation>(root);Ref(fx,"_visual",visual.transform);
            Refs(fx,"_petals",Enumerable.Range(0,3).Select(i=>visual.transform.Find("Petal_"+i)));
            Ref(fx,"_pulse",Line(p,"DetectionPulse"));Refs(fx,"_beams",Enumerable.Range(0,3).Select(i=>Line(p,"ConfirmedDischarge_"+i)));Refs(fx,"_impacts",Enumerable.Range(0,3).Select(i=>Line(p,"ConfirmedImpact_"+i)));
            Refs(fx,"_sparks",Enumerable.Range(0,3).Select(i=>Sparks(p,"ImpactSparks_"+i,16)));Ref(fx,"_breakSparks",Sparks(p,"DestroyedSparks",28));
            var audio=Child(p,"SpatialAudio").gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=1;audio.maxDistance=18;audio.rolloffMode=AudioRolloffMode.Linear;
            Ref(fx,"_audio",audio);Ref(fx,"_deploySound",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Drone/Unfold.wav"));Ref(fx,"_zapSound",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Drone/Confirmed.wav"));Ref(fx,"_endSound",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Drone/Interrupted.wav"));
            Ref(root.GetComponent<ScannerProjectile>(),"_presentation",fx);
            var box=root.GetComponent<BoxCollider>();box.size=new Vector3(.42f,.26f,.42f);box.center=new Vector3(0,.05f,0);
            var rb=root.GetComponent<Rigidbody>();rb.mass=.25f;rb.useGravity=true;rb.constraints=RigidbodyConstraints.FreezeRotation;
            PrefabUtility.SaveAsPrefabAsset(root,ProjectilePath);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    internal static AnimationClip Clip(GameObject root, bool ultimate = false)
    {
        var model=root.transform.Find("CameraRoot/Controller Model_Riging (1)");var camera=root.GetComponentInChildren<Camera>(true).transform;
        var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t==model || t.name=="Armature" || t.name.StartsWith("mixamorig:")).ToArray();
        var rotations=bones.Select(t=>t.localRotation).ToArray();var positions=bones.Select(t=>t.localPosition).ToArray();
        void Reset(){for(int i=0;i<bones.Length;i++){bones[i].localRotation=rotations[i];bones[i].localPosition=positions[i];}}
        Transform Bone(string name)=>bones.First(t=>t.name=="mixamorig:"+name);
        var upper=Bone("LeftArm");var fore=Bone("LeftForeArm");var hand=Bone("LeftHand");var right=Bone("RightArm");var rightFore=Bone("RightForeArm");
        var samples=new Quaternion[3][];var samplePositions=new Vector3[3][];
        var targets=ultimate ? new[]{new Vector3(-.23f,-.24f,.33f),new Vector3(-.17f,-.13f,.37f),new Vector3(-.09f,-.10f,.46f)} : new[]{new Vector3(-.10f,-.17f,.38f),new Vector3(-.17f,-.14f,.32f),new Vector3(.015f,-.07f,.43f)};
        for(int s=0;s<3;s++){
            Reset();model.localPosition+=model.parent.InverseTransformVector(camera.TransformVector(new Vector3(-.34f,-.10f,.13f)));
            upper.rotation=Quaternion.FromToRotation(fore.position-upper.position,camera.TransformDirection(new Vector3(-1,-.3f,.1f)))*upper.rotation;
            SkillArmPose.Reach(upper,fore,hand,camera,targets[s],1);
            var tip=hand.GetComponentsInChildren<Transform>(true).Last(t=>t.name.StartsWith("mixamorig:"));
            hand.rotation=Quaternion.FromToRotation(tip.position-hand.position,camera.TransformDirection(new Vector3(.85f,.1f,.6f)))*hand.rotation;
            right.rotation=Quaternion.FromToRotation(rightFore.position-right.position,camera.TransformDirection(new Vector3(.15f,-1,-.25f)))*right.rotation;
            if(ultimate){
                right.rotation=Quaternion.FromToRotation(rightFore.position-right.position,camera.TransformDirection(new Vector3(.7f,-1,-1.6f)))*right.rotation;
                var rightHand=Bone("RightHand");rightFore.rotation=Quaternion.FromToRotation(rightHand.position-rightFore.position,camera.TransformDirection(new Vector3(.3f,-1,-1)))*rightFore.rotation;
            }
            samples[s]=bones.Select(t=>t.localRotation).ToArray();samplePositions[s]=bones.Select(t=>t.localPosition).ToArray();
        }
        Reset();string clipName=ultimate?"Ultimate_Command_FP":"Scanner_Throw_FP";string path="Assets/Character Animation/Initiator/FirstPerson/"+clipName+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);bool create=!clip;if(create)clip=new AnimationClip();else clip.ClearCurves();
        clip.name=clipName;clip.frameRate=60;clip.wrapMode=WrapMode.ClampForever;
        for(int i=0;i<bones.Length;i++)for(int axis=0;axis<4;axis++){
            string bonePath=AnimationUtility.CalculateTransformPath(bones[i],model);var curve=new AnimationCurve();
            for(int s=0;s<3;s++)curve.AddKey(new Keyframe(s*.16f,samples[s][i][axis]));clip.SetCurve(bonePath,typeof(Transform),"localRotation."+"xyzw"[axis],curve);
            if(axis==3)continue;curve=new AnimationCurve();for(int s=0;s<3;s++)curve.AddKey(new Keyframe(s*.16f,samplePositions[s][i][axis]));clip.SetCurve(bonePath,typeof(Transform),"localPosition."+"xyz"[axis],curve);
        }
        clip.EnsureQuaternionContinuity();if(create)AssetDatabase.CreateAsset(clip,path);else EditorUtility.SetDirty(clip);return clip;
    }
    static void Actor()
    {
        var root=PrefabUtility.LoadPrefabContents(ActorPath);
        try{
            var rig=Component<ScannerCastRig>(root);Ref(root.GetComponent<InitiatorSkill_C>(),"_rig",rig);
            var motion=root.GetComponent<FirstPersonSkillMotion>();Ref(motion,"_scanner",Clip(root));Ref(rig,"_motion",motion);
            var fp=root.transform.Find("CameraRoot/Controller Model_Riging (1)");Ref(rig,"_viewModel",fp);
            var all=fp.GetComponentsInChildren<Transform>(true);var firstHand=all.First(t=>t.name=="mixamorig:LeftHand");Ref(rig,"_firstHand",firstHand);
            var model=root.transform.Find("Model");all=model.GetComponentsInChildren<Transform>(true);
            Transform Bone(string n)=>all.First(t=>t.name=="mixamorig:"+n || t.name.EndsWith("_"+n) || t.name==n);
            var hand=Bone("LeftHand");Ref(rig,"_worldHand",hand);
            var bones=new[]{Bone("LeftArm"),Bone("LeftForeArm"),hand,Bone("RightArm")};Refs(rig,"_worldBones",bones);
            var tip=hand.GetComponentsInChildren<Transform>(true).First(t=>t!=hand && t.name.IndexOf("Middle",StringComparison.OrdinalIgnoreCase)>=0);
            var ends=new[]{bones[1],hand,tip,Bone("RightForeArm")};var rest=bones.Select(t=>t.localRotation).ToArray();
            for(int s=0;s<2;s++){
                var dirs=s==0?new[]{new Vector3(-.1f,-.4f,.8f),new Vector3(.1f,.7f,.5f),new Vector3(.7f,.2f,.4f),new Vector3(.1f,-1,-.2f)}:new[]{new Vector3(-.1f,.1f,1),new Vector3(.1f,0,1),new Vector3(.8f,0,.6f),new Vector3(.1f,-1,-.2f)};
                for(int i=0;i<bones.Length;i++)bones[i].rotation=Quaternion.FromToRotation(ends[i].position-bones[i].position,root.transform.TransformDirection(dirs[i]))*bones[i].rotation;
                var data=new SerializedObject(rig);var a=data.FindProperty(s==0?"_readyPose":"_releasePose");a.arraySize=bones.Length;for(int i=0;i<bones.Length;i++)a.GetArrayElementAtIndex(i).quaternionValue=bones[i].localRotation;data.ApplyModifiedPropertiesWithoutUndo();
                for(int i=0;i<bones.Length;i++)bones[i].localRotation=rest[i];
            }
            var device=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/ScannerDevice.prefab");
            foreach(bool first in new[]{true,false}){
                var parent=first?fp.parent:model;string name=first?"ScannerHeld_FirstPerson":"ScannerHeld_World";RemoveChild(parent,name);
                var g=(GameObject)PrefabUtility.InstantiatePrefab(device,parent);g.name=name;
                float size=first?.42f:1;g.transform.localScale=Vector3.one*(size/Mathf.Max(.0001f,parent.lossyScale.x));
                // Bone objects use PlayerBody, which is deliberately culled from the owner's camera.
                int layer=first?LayerMask.NameToLayer("FirstPersonArms"):0;foreach(var t in g.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;
                g.SetActive(false);Ref(rig,first?"_firstDevice":"_worldDevice",g);
            }
            var line=root.GetComponent<LineRenderer>();line.enabled=false;line.positionCount=0;line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Drone/EnergyBeam.mat");line.startColor=line.endColor=new Color(.2f,.75f,.9f,.45f);line.widthCurve=AnimationCurve.Constant(0,1,1);line.widthMultiplier=.018f;
            PrefabUtility.SaveAsPrefabAsset(root,ActorPath);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Isolated batch editor required");
        Directory.CreateDirectory(Dir);AssetDatabase.Refresh();Device();Projectile();Actor();AssetDatabase.SaveAssets();Debug.Log("SCANNER_AUTHOR_PASS");
    }
    public static void Build(){Run();ScannerPolishVerification.Run();RankedCaptureTestEditor.Build();}
}
#endif
