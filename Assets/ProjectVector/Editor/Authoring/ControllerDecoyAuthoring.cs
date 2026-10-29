#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ControllerDecoyAuthoring
{
    public const string Dir="Assets/ProjectVector/Shared/Characters/Effects/Controller/Controller/Decoy", Actor="Assets/ProjectVector/Shared/Characters/Prefabs/Controller/Controller.prefab";
    public const string Trap="Assets/ProjectVector/Shared/Characters/Prefabs/Controller/DecoyTrap.prefab", Area="Assets/ProjectVector/Shared/Characters/Prefabs/Controller/DecoyTrapArea.prefab";
    static void Ref(Object o,string key,Object v){var s=new SerializedObject(o);s.FindProperty(key).objectReferenceValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
    static void ArrayRef(Object o,string key,Object[] values){var s=new SerializedObject(o);var a=s.FindProperty(key);a.arraySize=values.Length;for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();}
    static T Add<T>(GameObject g) where T:Component {var c=g.GetComponent<T>();return c?c:g.AddComponent<T>();}
    static Material Mat(string name,string shader,Color color)
    {
        string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.28f);
        EditorUtility.SetDirty(m);return m;
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
        // Existing controller throw foley, shared sound family; no swamp asset writes.
        if(!AssetDatabase.LoadAssetAtPath<AudioClip>(Dir+"/DecoyThrow.wav"))AssetDatabase.CopyAsset("Assets/ProjectVector/Shared/Characters/Effects/Controller/Controller/Swamp/SwampThrow.wav",Dir+"/DecoyThrow.wav");
        AttachSound();
        var silver=Mat("Shell","Universal Render Pipeline/Lit",new Color(.86f,.89f,.98f));
        var dark=Mat("Speaker","Universal Render Pipeline/Lit",new Color(.10f,.14f,.24f));
        var blue=Mat("Lens","Universal Render Pipeline/Lit",new Color(.59f,.70f,.96f));
        blue.EnableKeyword("_EMISSION");blue.SetColor("_EmissionColor",new Color(.24f,.35f,.65f));
        var pulse=Mat("Pulse","Vector/ControllerDecoyPulse",Color.white);
        Device(silver,dark,blue,pulse);Motion();
        AssetDatabase.SaveAssets();Debug.Log("DECOY_AUTHOR_PASS");
    }
    static GameObject Shape(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 scale,Material mat)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
        Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;return g;
    }
    static GameObject Model(Transform parent,Material silver,Material dark,Material blue)
    {
        var g=new GameObject("EchoTransmitter");g.transform.SetParent(parent,false);
        Shape(g.transform,"MagneticBase",PrimitiveType.Cylinder,new Vector3(0,.035f,0),new Vector3(.23f,.035f,.23f),dark);
        Shape(g.transform,"PorcelainHousing",PrimitiveType.Cylinder,new Vector3(0,.08f,0),new Vector3(.24f,.023f,.24f),silver);
        Shape(g.transform,"RecessedSpeaker",PrimitiveType.Cylinder,new Vector3(0,.108f,0),new Vector3(.18f,.012f,.18f),dark);
        Shape(g.transform,"Iris",PrimitiveType.Sphere,new Vector3(0,.128f,0),new Vector3(.07f,.025f,.07f),blue);
        for(int i=0;i<3;i++){
            var pivot=new GameObject("Wing"+i);pivot.transform.SetParent(g.transform,false);pivot.transform.localPosition=new Vector3(0,.07f,0);pivot.transform.localRotation=Quaternion.Euler(0,i*120,0);
            var p=new GameObject("SilverFin",typeof(MeshFilter),typeof(MeshRenderer));p.transform.SetParent(pivot.transform,false);
            p.GetComponent<MeshFilter>().sharedMesh=FinMesh();p.GetComponent<Renderer>().sharedMaterial=silver;
            Shape(pivot.transform,"SignalBar",PrimitiveType.Cube,new Vector3(0,.026f,.165f),new Vector3(.018f,.008f,.095f),blue);
            Shape(pivot.transform,"DarkInset",PrimitiveType.Cube,new Vector3(0,.021f,.108f),new Vector3(.09f,.012f,.018f),dark);
        }
        for(int i=0;i<8;i++){
            float a=i*Mathf.PI/4;var vent=Shape(g.transform,"SpeakerSlot"+i,PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*.064f,.123f,Mathf.Sin(a)*.064f),new Vector3(.012f,.006f,.025f),silver);vent.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
        }
        return g;
    }
    static Mesh FinMesh()
    {
        string path=Dir+"/BeveledFin.asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=!m;if(create)m=new Mesh();else m.Clear();
        Vector2[] outline={new Vector2(-.035f,.04f),new Vector2(-.088f,.13f),new Vector2(-.055f,.23f),new Vector2(0,.275f),new Vector2(.055f,.23f),new Vector2(.088f,.13f),new Vector2(.035f,.04f)};
        var v=new Vector3[outline.Length*2+2];var triangles=new System.Collections.Generic.List<int>();int n=outline.Length;
        for(int i=0;i<n;i++){v[i]=new Vector3(outline[i].x,0,outline[i].y);v[i+n]=new Vector3(outline[i].x*.86f,.024f,Mathf.Lerp(.15f,outline[i].y,.92f));}
        v[2*n]=new Vector3(0,0,.15f);v[2*n+1]=new Vector3(0,.024f,.15f);
        for(int i=0;i<n;i++){int j=(i+1)%n;triangles.AddRange(new[]{i,j,n+i,j,n+j,n+i,2*n,j,i,2*n+1,n+i,n+j});}
        m.name="BeveledEchoFin";m.vertices=v;m.triangles=triangles.ToArray();m.RecalculateNormals();m.RecalculateBounds();
        if(create)AssetDatabase.CreateAsset(m,path);else EditorUtility.SetDirty(m);return m;
    }
    static void Device(Material silver,Material dark,Material blue,Material pulse)
    {
        var root=PrefabUtility.LoadPrefabContents(Trap);
        try{
            for(int i=root.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach(var c in root.GetComponents<Collider>())Object.DestroyImmediate(c);
            var model=Model(root.transform,silver,dark,blue);
            var col=root.AddComponent<SphereCollider>();col.radius=.18f;col.center=new Vector3(0,.08f,0);
            var rb=root.GetComponent<Rigidbody>();rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.interpolation=RigidbodyInterpolation.Interpolate;
            PrefabUtility.SaveAsPrefabAsset(model,Dir+"/HeldDecoy.prefab");PrefabUtility.SaveAsPrefabAsset(root,Trap);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        root=PrefabUtility.LoadPrefabContents(Area);
        try{
            for(int i=root.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var decal=root.GetComponent<UnityEngine.Rendering.Universal.DecalProjector>();if(decal)Object.DestroyImmediate(decal);
            foreach(var c in root.GetComponents<Collider>())Object.DestroyImmediate(c);
            var model=Model(root.transform,silver,dark,blue);
            var fx=Add<ControllerDecoyVfx>(root);
            ArrayRef(fx,"_panels",model.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Wing")).Cast<Object>().ToArray());
            ArrayRef(fx,"_lenses",model.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial==blue).Cast<Object>().ToArray());
            var wave=Shape(root.transform,"LocalSoundPulse",PrimitiveType.Quad,new Vector3(0,.035f,0),Vector3.one*1.3f,pulse);wave.transform.localRotation=Quaternion.Euler(90,0,0);wave.GetComponent<Renderer>().enabled=false;Ref(fx,"_wave",wave.GetComponent<Renderer>());
            Ref(fx,"_landing",Burst(root.transform));
            var attach=new GameObject("MagneticLatchSound");attach.transform.SetParent(root.transform,false);Ref(fx,"_attachSound",Audio(attach,"DecoyLatch",true));
            var gun=Add<AudioSource>(root);gun.playOnAwake=false;gun.loop=false;gun.clip=null;gun.spatialBlend=1;gun.rolloffMode=AudioRolloffMode.Logarithmic;gun.minDistance=3;gun.maxDistance=80;gun.dopplerLevel=0;gun.volume=.42f;
            var area=root.GetComponent<DecoyTrapArea>();Ref(area,"_audioSource",gun);Ref(area,"_gunfireClip",AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ProjectVector/Shared/Weapons/Assets/Pistol/P92/P92.asset").gunSound);Ref(area,"_muzzleFlash",null);Ref(area,"_vfx",fx);
            var s=new SerializedObject(area);s.FindProperty("_duration").floatValue=6;s.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Area);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static ParticleSystem Burst(Transform parent)
    {
        var g=new GameObject("LatchParticles");g.transform.SetParent(parent,false);g.transform.localPosition=Vector3.up*.1f;g.transform.localRotation=Quaternion.Euler(-90,0,0);
        var ps=g.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=ps.main;m.loop=false;m.playOnAwake=false;m.duration=.3f;m.startLifetime=.23f;m.startSpeed=.7f;m.startSize=.035f;m.startColor=new Color(.8f,.88f,1,.6f);m.maxParticles=20;
        var e=ps.emission;e.rateOverTime=0;e.SetBursts(new[]{new ParticleSystem.Burst(0,12)});
        var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Cone;sh.angle=70;sh.radius=.1f;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/ProjectVector/Shared/Characters/Effects/Controller/Controller/Swamp/SwampMist.mat");return ps;
    }
    static AudioSource Audio(GameObject g,string name,bool spatial)
    {
        var a=g.AddComponent<AudioSource>();a.playOnAwake=false;a.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Dir+"/"+name+".wav");a.spatialBlend=spatial?1:0;a.volume=.35f;a.minDistance=2;a.maxDistance=22;a.dopplerLevel=0;return a;
    }
    static void AttachSound()
    {
        const int rate=24000,count=7200;string path=Dir+"/DecoyLatch.wav";
        using(var w=new BinaryWriter(File.Create(path))){
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
            var rng=new System.Random(181);float low=0;
            for(int i=0;i<count;i++){float t=i/(float)rate;low=Mathf.Lerp(low,(float)rng.NextDouble()*2-1,.4f);float click=low*Mathf.Exp(-t*65)*.6f;float ring=Mathf.Sin(t*2*Mathf.PI*880)*Mathf.Exp(-t*22)*.14f;w.Write((short)(Mathf.Clamp(click+ring,-1,1)*29000));}
        }AssetDatabase.ImportAsset(path);
    }
    static void Motion()
    {
        var root=PrefabUtility.LoadPrefabContents(Actor);
        try{
            var model=root.transform.Find("CameraRoot/Controller Model_Riging (1)");var camera=root.GetComponent<PlayerCameraController>().PlayerCamera.transform;
            var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t==model||t.name=="Armature"||t.name.StartsWith("mixamorig:")).ToArray();
            var rotations=bones.Select(t=>t.localRotation).ToArray();var positions=bones.Select(t=>t.localPosition).ToArray();
            Transform Bone(string name)=>bones.First(t=>t.name=="mixamorig:"+name);
            void Reset(){for(int i=0;i<bones.Length;i++){bones[i].localRotation=rotations[i];bones[i].localPosition=positions[i];}}
            float[] times={0,.18f,.38f,.48f,.72f};
            var targets=new[]{new Vector3(.24f,-.26f,.32f),new Vector3(.26f,-.12f,.32f),new Vector3(.19f,-.10f,.55f),new Vector3(.12f,-.09f,.63f),new Vector3(.28f,-.30f,.32f)};
            var rs=new Quaternion[5][];var ps=new Vector3[5][];
            for(int f=0;f<5;f++){
                Reset();model.localPosition+=model.parent.InverseTransformVector(camera.TransformVector(new Vector3(.10f,-.17f,.20f)));
                var upper=Bone("RightArm");var fore=Bone("RightForeArm");var hand=Bone("RightHand");
                upper.rotation=Quaternion.FromToRotation(fore.position-upper.position,camera.TransformDirection(new Vector3(1,-.5f,.15f)))*upper.rotation;
                SkillArmPose.Reach(upper,fore,hand,camera,targets[f],1);
                var finger=hand.GetComponentsInChildren<Transform>(true).Last(t=>t.name.StartsWith("mixamorig:"));hand.rotation=Quaternion.FromToRotation(finger.position-hand.position,camera.TransformDirection(new Vector3(-.1f,.12f,1)))*hand.rotation;
                var left=Bone("LeftArm");left.rotation=Quaternion.FromToRotation(Bone("LeftForeArm").position-left.position,camera.TransformDirection(new Vector3(-.3f,-1,-.35f)))*left.rotation;
                rs[f]=bones.Select(t=>t.localRotation).ToArray();ps[f]=bones.Select(t=>t.localPosition).ToArray();
            }
            Reset();string path=Dir+"/DecoyThrow_RightHand.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);bool create=!clip;if(create)clip=new AnimationClip();else clip.ClearCurves();clip.name="DecoyThrow_RightHand";clip.frameRate=60;
            for(int i=0;i<bones.Length;i++)for(int axis=0;axis<4;axis++){
                string bp=AnimationUtility.CalculateTransformPath(bones[i],model);
                clip.SetCurve(bp,typeof(Transform),"localRotation."+"xyzw"[axis],new AnimationCurve(Enumerable.Range(0,5).Select(f=>new Keyframe(times[f],rs[f][i][axis])).ToArray()));
                if(axis<3)clip.SetCurve(bp,typeof(Transform),"localPosition."+"xyz"[axis],new AnimationCurve(Enumerable.Range(0,5).Select(f=>new Keyframe(times[f],ps[f][i][axis])).ToArray()));
            }
            clip.EnsureQuaternionContinuity();if(create)AssetDatabase.CreateAsset(clip,path);else EditorUtility.SetDirty(clip);
            var handBone=Bone("RightHand");var old=handBone.Find("HeldDecoy");if(old)Object.DestroyImmediate(old.gameObject);
            var prop=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/HeldDecoy.prefab"));prop.name="HeldDecoy";prop.transform.SetParent(handBone,false);prop.transform.localPosition=new Vector3(0,.045f,0);prop.transform.localScale=Vector3.one*.30f;
            // Present the speaker face to the camera at the ready pose, keeping bone lengths intact.
            clip.SampleAnimation(model.gameObject,.18f);
            var tips=handBone.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig:")&&t.childCount==0).ToArray();
            if(tips.Length>0){
                Vector3 tip=Vector3.zero;foreach(var t in tips)tip+=t.position;tip/=tips.Length;
                prop.transform.position=Vector3.Lerp(handBone.position,tip,.6f)-camera.forward*.025f;
            }
            prop.transform.rotation=Quaternion.LookRotation(camera.up,-camera.forward);
            Reset();
            foreach(var child in prop.GetComponentsInChildren<Transform>(true))child.gameObject.layer=model.gameObject.layer;prop.SetActive(false);
            var motion=Add<ControllerDecoyMotion>(root);Ref(motion,"_model",model);Ref(motion,"_clip",clip);Ref(motion,"_device",prop);
            var s=new SerializedObject(motion);var array=s.FindProperty("_bones");array.arraySize=bones.Length;for(int i=0;i<bones.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=bones[i];s.ApplyModifiedPropertiesWithoutUndo();
            var soundRoot=root.transform.Find("DecoyThrowSound");if(!soundRoot){soundRoot=new GameObject("DecoyThrowSound").transform;soundRoot.SetParent(root.transform,false);Audio(soundRoot.gameObject,"DecoyThrow",false);}Ref(motion,"_audio",soundRoot.GetComponent<AudioSource>());
            Ref(root.GetComponent<ControllerSkill_C>(),"_motion",motion);PrefabUtility.SaveAsPrefabAsset(root,Actor);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
