#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Only swamp assets and the Q presentation references are authored here. No smoke or scene writes.
public static class ControllerSwampAuthoring
{
    public const string Dir="Assets/Art/Controller/Swamp", Actor="Assets/Prefab/Character/Controller/Controller.prefab";
    public const string Trap="Assets/Prefab/Character/Controller/Trap.prefab", Area="Assets/Prefab/Character/Controller/TrapArea.prefab";
    static void Ref(Object o,string key,Object v){var s=new SerializedObject(o);s.FindProperty(key).objectReferenceValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
    static T Add<T>(GameObject g) where T:Component { var c=g.GetComponent<T>();return c?c:g.AddComponent<T>(); }
    static Material Mat(string name,string shader,Color color)
    {
        string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.22f);
        EditorUtility.SetDirty(m);return m;
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
        Sound("SwampThrow",.35f,0);Sound("SwampBloom",.9f,1);Sound("SwampSlow",.4f,2);
        var body=Mat("DeviceSilver","Universal Render Pipeline/Lit",new Color(.83f,.87f,.97f));
        var dark=Mat("DeviceInset","Universal Render Pipeline/Lit",new Color(.12f,.17f,.28f));
        var blue=Mat("DeviceLight","Universal Render Pipeline/Lit",new Color(.55f,.68f,.96f));
        blue.EnableKeyword("_EMISSION");blue.SetColor("_EmissionColor",new Color(.23f,.35f,.6f));
        var surface=Mat("SwampSurface","Vector/ControllerSwamp",Color.white);
        var feet=Mat("SwampContact","Vector/ControllerSwamp",Color.white);feet.SetFloat("_Mode",1);
        var particle=Mat("SwampMist","Universal Render Pipeline/Particles/Unlit",Color.white);
        MistTexture();particle.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/Mist.png"));
        particle.SetFloat("_Surface",1);particle.SetFloat("_SrcBlend",5);particle.SetFloat("_DstBlend",10);particle.SetFloat("_ZWrite",0);particle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particle.renderQueue=3000;
        Device(body,dark,blue,particle);Contact(feet,particle);Ground(surface,particle);Motion();
        AssetDatabase.SaveAssets();Debug.Log("SWAMP_AUTHOR_PASS");
    }
    static GameObject Shape(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 scale,Material mat)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
        Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;return g;
    }
    static void Device(Material body,Material dark,Material blue,Material particle)
    {
        var root=PrefabUtility.LoadPrefabContents(Trap);
        try{
            for(int i=root.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var visual=new GameObject("SwampCapsule");visual.transform.SetParent(root.transform,false);
            Shape(visual.transform,"PressureCore",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.26f,.04f,.26f),dark);
            var shell=new GameObject("BeveledShell",typeof(MeshFilter),typeof(MeshRenderer));shell.transform.SetParent(visual.transform,false);shell.GetComponent<MeshFilter>().sharedMesh=ShellMesh();shell.GetComponent<MeshRenderer>().sharedMaterial=body;
            Shape(visual.transform,"BlueLens",PrimitiveType.Sphere,new Vector3(0,.063f,0),new Vector3(.11f,.02f,.11f),blue);
            for(int i=0;i<6;i++){
                float angle=i*60;var direction=Quaternion.Euler(0,angle,0)*Vector3.forward;
                var light=Shape(visual.transform,"FlowVent"+i,PrimitiveType.Cube,direction*.156f,new Vector3(.045f,.018f,.008f),blue);light.transform.localRotation=Quaternion.Euler(0,angle,0);
            }
            var col=Add<SphereCollider>(root);col.radius=.17f;
            var physics=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Dir+"/SwampBounce.physicMaterial");
            if(!physics){physics=new PhysicsMaterial("SwampBounce");AssetDatabase.CreateAsset(physics,Dir+"/SwampBounce.physicMaterial");}
            physics.bounciness=.65f;physics.bounceCombine=PhysicsMaterialCombine.Maximum;physics.dynamicFriction=.15f;physics.staticFriction=.15f;EditorUtility.SetDirty(physics);col.sharedMaterial=physics;
            var trail=Add<TrailRenderer>(root);trail.sharedMaterial=particle;trail.time=.16f;trail.startWidth=.055f;trail.endWidth=0;trail.startColor=new Color(.75f,.84f,1,.45f);trail.endColor=new Color(.75f,.84f,1,0);trail.minVertexDistance=.03f;
            PrefabUtility.SaveAsPrefabAsset(root,Trap);
            // A collider-free authored hand prop shares the same geometry.
            PrefabUtility.SaveAsPrefabAsset(visual,Dir+"/HeldSwamp.prefab");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static ParticleSystem Mist(Transform parent,Material material,bool burst)
    {
        var g=new GameObject(burst?"LandingMist":"AnkleMist");g.transform.SetParent(parent,false);g.transform.localRotation=Quaternion.Euler(-90,0,0);
        var p=g.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=p.main;main.loop=!burst;main.playOnAwake=!burst;main.duration=1;main.startLifetime=new ParticleSystem.MinMaxCurve(.3f,.65f);main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,burst?1.2f:.45f);main.startSize=new ParticleSystem.MinMaxCurve(.1f,.3f);main.startColor=new Color(.78f,.84f,1,.45f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=80;
        var emission=p.emission;emission.rateOverTime=burst?0:14;if(burst)emission.SetBursts(new[]{new ParticleSystem.Burst(0,28)});
        var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=burst?78:30;shape.radius=burst?.18f:.3f;
        var color=p.colorOverLifetime;color.enabled=true;var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.65f,.73f,.9f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.6f,.12f),new GradientAlphaKey(0,1)});color.color=grad;
        p.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;return p;
    }
    static void Contact(Material material,Material particles)
    {
        var root=new GameObject("SwampContact");
        try{
            var disk=Shape(root.transform,"FootFlow",PrimitiveType.Quad,Vector3.zero,Vector3.one*.95f,material);disk.transform.localRotation=Quaternion.Euler(90,0,0);
            Mist(root.transform,particles,false);PrefabUtility.SaveAsPrefabAsset(root,Dir+"/SwampContact.prefab");
        }finally{Object.DestroyImmediate(root);}
    }
    static AudioSource Audio(GameObject g,string name,bool spatial)
    {
        var a=g.AddComponent<AudioSource>();a.playOnAwake=false;a.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Dir+"/"+name+".wav");a.spatialBlend=spatial?1:0;a.volume=.4f;a.minDistance=2;a.maxDistance=25;return a;
    }
    static void Ground(Material material,Material particles)
    {
        var root=PrefabUtility.LoadPrefabContents(Area);
        try{
            var renderer=root.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var visual=new SerializedObject(root.GetComponent<SwampGroundMeshVisual>());visual.FindProperty("_randomEdge").boolValue=false;visual.FindProperty("_segments").intValue=96;visual.FindProperty("_rings").intValue=12;visual.ApplyModifiedPropertiesWithoutUndo();
            var area=root.GetComponent<SwampArea>();Ref(area,"_affectedEffect",AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/SwampContact.prefab"));Ref(area,"_entrySound",AssetDatabase.LoadAssetAtPath<AudioClip>(Dir+"/SwampSlow.wav"));
            var fx=Add<ControllerSwampVfx>(root);Ref(fx,"_surface",renderer);
            if(!root.transform.Find("LandingMist"))Ref(fx,"_bloom",Mist(root.transform,particles,true));
            var a=root.GetComponent<AudioSource>();if(!a)a=Audio(root,"SwampBloom",true);Ref(fx,"_audio",a);
            PrefabUtility.SaveAsPrefabAsset(root,Area);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
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
            Reset();string path=Dir+"/SwampThrow_RightHand.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);bool create=!clip;if(create)clip=new AnimationClip();else clip.ClearCurves();clip.name="SwampThrow_RightHand";clip.frameRate=60;
            for(int i=0;i<bones.Length;i++)for(int axis=0;axis<4;axis++){
                string bp=AnimationUtility.CalculateTransformPath(bones[i],model);
                clip.SetCurve(bp,typeof(Transform),"localRotation."+"xyzw"[axis],new AnimationCurve(Enumerable.Range(0,5).Select(f=>new Keyframe(times[f],rs[f][i][axis])).ToArray()));
                if(axis<3)clip.SetCurve(bp,typeof(Transform),"localPosition."+"xyz"[axis],new AnimationCurve(Enumerable.Range(0,5).Select(f=>new Keyframe(times[f],ps[f][i][axis])).ToArray()));
            }
            clip.EnsureQuaternionContinuity();if(create)AssetDatabase.CreateAsset(clip,path);else EditorUtility.SetDirty(clip);
            var handBone=Bone("RightHand");var old=handBone.Find("HeldSwamp");if(old)Object.DestroyImmediate(old.gameObject);
            var prop=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/HeldSwamp.prefab"));prop.name="HeldSwamp";prop.transform.SetParent(handBone,false);prop.transform.localPosition=new Vector3(0,.045f,0);prop.transform.localScale=Vector3.one*.45f;
            foreach(var child in prop.GetComponentsInChildren<Transform>(true))child.gameObject.layer=model.gameObject.layer;prop.SetActive(false);
            var motion=Add<ControllerSwampMotion>(root);Ref(motion,"_model",model);Ref(motion,"_clip",clip);Ref(motion,"_device",prop);
            var s=new SerializedObject(motion);var array=s.FindProperty("_bones");array.arraySize=bones.Length;for(int i=0;i<bones.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=bones[i];s.ApplyModifiedPropertiesWithoutUndo();
            var soundRoot=root.transform.Find("SwampThrowSound");if(!soundRoot){soundRoot=new GameObject("SwampThrowSound").transform;soundRoot.SetParent(root.transform,false);Audio(soundRoot.gameObject,"SwampThrow",false);}Ref(motion,"_audio",soundRoot.GetComponent<AudioSource>());
            Ref(root.GetComponent<ControllerSkill_Q>(),"_motion",motion);PrefabUtility.SaveAsPrefabAsset(root,Actor);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void MistTexture()
    {
        var t=new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;t.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2)));}
        t.Apply();string path=Dir+"/Mist.png";File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
    }
    static Mesh ShellMesh()
    {
        string path=Dir+"/CapsuleShell.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=!mesh;if(create)mesh=new Mesh();else mesh.Clear();
        const int sides=12;float[] radii={.12f,.155f,.155f,.12f};float[] heights={-.055f,-.025f,.025f,.055f};
        var vertices=new Vector3[4*sides+2];var uv=new Vector2[vertices.Length];var triangles=new System.Collections.Generic.List<int>();
        for(int r=0;r<4;r++)for(int i=0;i<sides;i++){float a=i*2*Mathf.PI/sides;vertices[r*sides+i]=new Vector3(Mathf.Cos(a)*radii[r],heights[r],Mathf.Sin(a)*radii[r]);uv[r*sides+i]=new Vector2(i/(float)sides,r/3f);}
        vertices[48]=Vector3.up*heights[0];vertices[49]=Vector3.up*heights[3];
        for(int i=0;i<sides;i++){int j=(i+1)%sides;for(int r=0;r<3;r++){int a=r*sides+i,b=r*sides+j,c=(r+1)*sides+i,d=(r+1)*sides+j;triangles.AddRange(new[]{a,c,b,b,c,d});}triangles.AddRange(new[]{48,i,j,49,36+j,36+i});}
        mesh.name="BeveledSwampCapsule";mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);return mesh;
    }
    static void Sound(string name,float duration,int mode)
    {
        int rate=24000,count=(int)(rate*duration);string path=Dir+"/"+name+".wav";
        using(var w=new BinaryWriter(File.Create(path))){w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);var rng=new System.Random(852+mode);float low=0;
            for(int i=0;i<count;i++){float t=i/(float)rate;low=Mathf.Lerp(low,(float)rng.NextDouble()*2-1,mode==0?.18f:.06f);float env=Mathf.Min(1,t/.015f)*Mathf.Pow(1-t/duration,1.8f);float tone=Mathf.Sin(2*Mathf.PI*((mode==2?420:120)*t-40*t*t))*Mathf.Exp(-t*9);w.Write((short)(Mathf.Clamp((low*.6f+tone*.2f)*env,-1,1)*28000));}}
        AssetDatabase.ImportAsset(path);
    }
}
#endif
