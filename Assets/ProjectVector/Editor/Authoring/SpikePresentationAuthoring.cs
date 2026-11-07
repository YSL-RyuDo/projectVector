#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static partial class SpikePresentationAuthoring
{
    public const string Folder = "Assets/ProjectVector/Modules/Match/Device/Art";
    public const string ModelPath = Folder + "/VectorSpike.prefab";
    public const string EffectPath = Folder + "/SpikeDetonation.prefab";
    static Material shell, dark, steel, light, warning, glass, fx;
    static Mesh bevel, cylinder, ring;
    static AudioClip equip, armed, beep, defuse, half, safe, burst;
    public static readonly string[] Characters = { "Initiator", "Controller", "Sentinel", "Entry" };
    public static void RunAndReview(){Run();SpikePresentationReview.Stills();}
    public static void Inspect()
    {
        foreach(string c in Characters)
        {
            var r=PrefabUtility.LoadPrefabContents("Assets/ProjectVector/Shared/Characters/Prefabs/"+c+"/"+c+".prefab");
            foreach(string path in new[]{"Model","CameraRoot/Controller Model_Riging (1)"})
            {
                var model=r.transform.Find(path);
                var bones=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(s=>s.bones).Where(b=>b).Distinct().ToArray();
                Debug.Log("RIG_INSPECT "+c+" "+path+" scale="+model.lossyScale+" bones="+string.Join(",",bones.Select(b=>b.name)));
                var camera=r.GetComponentInChildren<Camera>(true).transform;
                foreach(var b in bones.Where(b=>b.name.EndsWith("Arm")||b.name.EndsWith("Hand")))Debug.Log("RIG_POINT "+b.name+" "+camera.InverseTransformPoint(b.position));
            }
            PrefabUtility.UnloadPrefabContents(r);
        }
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Use batch with the project closed");
        ControllerCoatAuthoring.Run();
        Directory.CreateDirectory(Folder); Directory.CreateDirectory(Folder + "/Animations"); AssetDatabase.Refresh();
        BuildAssets();
        BuildAudioV2();
        BuildNetworkDevice(); BuildEffectV2();
        UpgradeWorld("Assets/ProjectVector/Modules/Match/Resources/Prefabs/Spike/Spike.prefab", true);
        UpgradeWorld("Assets/ProjectVector/Modules/Match/Resources/Prefabs/Spike/DroppedSpike.prefab", false);
        foreach (string character in Characters) AuthorCharacter("Assets/ProjectVector/Shared/Characters/Prefabs/" + character + "/" + character + ".prefab", character);
        AssetDatabase.SaveAssets();
        Verify();
        Debug.Log("SPIKE_AUTHOR_PASS");
    }
    static T Save<T>(T asset, string name) where T : Object
    {
        string path = Folder + "/" + name;
        var old = AssetDatabase.LoadAssetAtPath<T>(path);
        if (old) { EditorUtility.CopySerialized(asset, old); Object.DestroyImmediate(asset); return old; }
        AssetDatabase.CreateAsset(asset, path); return asset;
    }
    static Material Mat(string name, Color color, bool emission = false, bool transparent = false)
    {
        var m = new Material(Shader.Find(transparent ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
        m.name = name; m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", .29f); m.SetFloat("_Metallic", .23f);
        if (emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 1.8f); }
        if (transparent)
        {
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_Cull", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
        }
        return Save(m, name + ".mat");
    }
    static void BuildAssets()
    {
        shell = Mat("Ceramic", new Color(.72f, .79f, .84f));
        dark = Mat("Graphite", new Color(.055f, .08f, .115f));
        steel = Mat("Titanium", new Color(.2f, .28f, .34f));
        light = Mat("CoreCyan", SpikePresentation.Cyan, true);
        warning = Mat("SafetyAmber", new Color(1, .52f, .12f), true);
        glass = Mat("Display", new Color(.015f, .065f, .09f), true);
        fx = Mat("Energy", new Color(.3f, .8f, 1, .45f), false, true);
        bevel = Save(BevelMesh(), "Bevel.asset");
        cylinder = Save(PrismMesh(12), "Prism.asset");
        ring = Save(RingMesh(), "Ring.asset");
        equip = Sound("Equip", .45f, 0); armed = Sound("Armed", .7f, 1); beep = Sound("Pulse", .12f, 2);
        defuse = Sound("Defuse", .8f, 3); half = Sound("Checkpoint", .45f, 4); safe = Sound("Safe", .9f, 5); burst = Sound("Detonate", 3.4f, 6);
    }
    // Chamfered faces, flat normals. Geometry is saved as editable native assets.
    static Mesh BevelMesh()
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        Vector2[] cross = {new(-.4f,-.5f),new(.4f,-.5f),new(.5f,-.4f),new(.5f,.4f),new(.4f,.5f),new(-.4f,.5f),new(-.5f,.4f),new(-.5f,-.4f)};
        void Face(params Vector3[] points)
        {
            int start = vertices.Count; vertices.AddRange(points);
            for (int i = 1; i < points.Length - 1; i++) { triangles.Add(start); triangles.Add(start+i); triangles.Add(start+i+1); }
        }
        var front=cross.Select(p=>new Vector3(p.x,p.y,-.5f)).Reverse().ToArray();
        var back=cross.Select(p=>new Vector3(p.x,p.y,.5f)).ToArray(); Face(front); Face(back);
        for (int i=0;i<8;i++){int j=(i+1)%8;Face(new(cross[i].x,cross[i].y,-.5f),new(cross[j].x,cross[j].y,-.5f),new(cross[j].x,cross[j].y,.5f),new(cross[i].x,cross[i].y,.5f));}
        var m=new Mesh{name="Chamfered housing"};m.SetVertices(vertices);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();return m;
    }
    static Mesh PrismMesh(int sides)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
            Vector3 p=new(Mathf.Cos(a)*.5f,0,Mathf.Sin(a)*.5f),q=new(Mathf.Cos(b)*.5f,0,Mathf.Sin(b)*.5f);
            int n=v.Count;v.AddRange(new[]{p-Vector3.up*.5f,p+Vector3.up*.5f,q+Vector3.up*.5f,q-Vector3.up*.5f});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            n=v.Count;v.AddRange(new[]{Vector3.up*.5f,p+Vector3.up*.5f,q+Vector3.up*.5f});t.AddRange(new[]{n,n+2,n+1});
            n=v.Count;v.AddRange(new[]{-Vector3.up*.5f,p-Vector3.up*.5f,q-Vector3.up*.5f});t.AddRange(new[]{n,n+1,n+2});
        }
        var m=new Mesh{name="Machined prism"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();return m;
    }
    static Mesh RingMesh()
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<96;i++)
        {
            float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96;int n=v.Count;
            v.AddRange(new[]{new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),new Vector3(Mathf.Cos(b),0,Mathf.Sin(b)),new Vector3(Mathf.Cos(b)*.976f,0,Mathf.Sin(b)*.976f),new Vector3(Mathf.Cos(a)*.976f,0,Mathf.Sin(a)*.976f)});
            t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
        }
        var m=new Mesh{name="Energy wave"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();return m;
    }
    static GameObject Part(string name, Transform parent, Vector3 pos, Vector3 size, Material material, Mesh mesh=null)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;
        g.AddComponent<MeshFilter>().sharedMesh=mesh?mesh:bevel;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;
        return g;
    }
    static Transform Node(string name, Transform parent, Vector3 pos)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;return t;
    }
    static void BuildModel()
    {
        var root=new GameObject("Vector Spike");var t=root.transform;var visual=root.AddComponent<SpikeDeviceVisual>();
        Part("Foundation",t,new(0,.035f,0),new(.30f,.07f,.25f),dark);
        Part("Lower silver rail",t,new(0,.079f,0),new(.285f,.018f,.235f),steel);
        Part("Core socket",t,new(0,.115f,0),new(.17f,.07f,.17f),steel,cylinder);
        var core=Node("Rising core",t,new(0,.22f,0));visual.core=core;
        Part("Luminous cartridge",core,Vector3.zero,new(.11f,.25f,.11f),light,cylinder);
        Part("Core cap",core,new(0,.135f,0),new(.15f,.035f,.15f),dark,cylinder);
        Part("Cap halo",core,new(0,.155f,0),new(.12f,.012f,.12f),light,cylinder);
        for(int i=0;i<4;i++)
        {
            float a=i*Mathf.PI*.5f;
            Part("Core brace "+i,core,new(Mathf.Cos(a)*.066f,0,Mathf.Sin(a)*.066f),new(.018f,.28f,.018f),steel);
        }
        visual.petals=new Transform[4];
        for(int i=0;i<4;i++)
        {
            var hinge=Node("Deployable shield "+i,t,Vector3.zero);hinge.localRotation=Quaternion.Euler(0,i*90,0);visual.petals[i]=hinge;
            Part("Ceramic armor",hinge,new(.116f,.22f,0),new(.075f,.27f,.18f),shell);
            Part("Outer graphite inset",hinge,new(.157f,.225f,0),new(.012f,.17f,.13f),dark);
            Part("Status spine",hinge,new(.165f,.23f,0),new(.008f,.13f,.017f),light);
            Part("Hinge pin",hinge,new(.10f,.075f,0),new(.065f,.04f,.21f),steel);
            for(int j=0;j<3;j++) Part("Vent "+j,hinge,new(.165f,.13f+j*.018f,.04f),new(.009f,.008f,.04f),steel);
        }
        Part("Control console",t,new(0,.15f,-.15f),new(.21f,.14f,.045f),dark);
        Part("Screen bezel",t,new(0,.175f,-.176f),new(.15f,.063f,.008f),steel);
        Part("Screen glass",t,new(0,.175f,-.181f),new(.13f,.048f,.006f),glass);
        for(int i=0;i<5;i++) Part("Charge segment "+i,t,new(-.047f+i*.023f,.176f,-.186f),new(.012f,.025f,.003f),light);
        for(int i=0;i<3;i++) Part("Key "+i,t,new(-.055f+i*.055f,.118f,-.177f),new(.032f,.017f,.013f),i==2?warning:shell);
        for(int s=-1;s<=1;s+=2)
        {
            Part("Handle "+s,t,new(s*.17f,.19f,-.016f),new(.025f,.16f,.055f),dark);
            Part("Grip trim "+s,t,new(s*.17f,.19f,-.05f),new(.026f,.11f,.012f),steel);
        }
        visual.lamps=root.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial==light).ToArray();
        PrefabUtility.SaveAsPrefabAsset(root,ModelPath);Object.DestroyImmediate(root);
    }
    static AudioSource Audio(GameObject root)
    {
        var s=root.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=1;s.rolloffMode=AudioRolloffMode.Linear;s.minDistance=2;s.maxDistance=30;s.dopplerLevel=0;return s;
    }
    static AudioClip Sound(string name,float seconds,int kind)
    {
        const int rate=44100;int n=(int)(rate*seconds);var random=new System.Random(140+kind);var data=new float[n];float filtered=0;
        for(int i=0;i<n;i++)
        {
            float t=(float)i/rate,u=t/seconds;float noise=(float)random.NextDouble()*2-1;filtered=Mathf.Lerp(filtered,noise,kind==6?.09f:.5f);
            float envelope=Mathf.Min(1,t*160)*Mathf.Pow(1-u,kind==6?2:1.5f);
            float f=kind==2?1150:kind==5?Mathf.Lerp(900,460,u):kind==1?Mathf.Lerp(280,1000,u):kind==4?1350:kind==3?Mathf.Lerp(420,650,u):190;
            float tone=Mathf.Sin(2*Mathf.PI*f*t)*.38f+Mathf.Sin(2*Mathf.PI*f*1.5f*t)*.12f;
            if(kind==6)tone=filtered*1.25f+Mathf.Sin(2*Mathf.PI*(48*t+26*(1-Mathf.Exp(-t*4))))*.4f;
            if(kind==0)tone=filtered*.55f*Mathf.Exp(-t*12)+tone*.12f;
            if(kind==3)tone=filtered*.12f+tone*(.55f+.45f*Mathf.Sin(t*44));
            data[i]=Mathf.Clamp(tone*envelope*.8f,-1,1);
        }
        string path=Folder+"/"+name+".wav";
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+n*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(n*2);
            foreach(float sample in data)writer.Write((short)(sample*32767));
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    static void BuildEffect()
    {
        var root=new GameObject("Spike energy collapse");var e=root.AddComponent<SpikeDetonation>();
        e.shockRing=Part("Ground shock wave",root.transform,new(0,.035f,0),Vector3.one,fx,ring).transform;
        e.innerRing=Part("Secondary wave",root.transform,new(0,.11f,0),Vector3.one,fx,ring).transform;
        var energy=new Material(Shader.Find("Vector/SpikeEnergy"));energy.SetColor("_BaseColor",new Color(.3f,.8f,1,.6f));energy=Save(energy,"DetonationShell.mat");
        var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="Expanding energy shell";sphere.transform.SetParent(root.transform,false);sphere.transform.localPosition=Vector3.up*.3f;Object.DestroyImmediate(sphere.GetComponent<Collider>());sphere.GetComponent<Renderer>().sharedMaterial=energy;e.core=sphere.transform;
        e.fading=root.GetComponentsInChildren<Renderer>();e.source=Audio(root);e.source.maxDistance=75;e.burst=burst;
        var particle=new GameObject("Core fragments");particle.transform.SetParent(root.transform,false);particle.transform.localPosition=Vector3.up*.3f;
        e.sparks=particle.AddComponent<ParticleSystem>();var main=e.sparks.main;main.playOnAwake=false;main.loop=false;main.duration=.15f;main.startLifetime=new ParticleSystem.MinMaxCurve(.5f,1.3f);main.startSpeed=new ParticleSystem.MinMaxCurve(3,9);main.startSize=new ParticleSystem.MinMaxCurve(.02f,.07f);main.startColor=new Color(.3f,.82f,1);main.gravityModifier=.65f;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=e.sparks.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,70)});var shape=e.sparks.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.2f;
        var renderer=particle.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=fx;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=3;
        PrefabUtility.SaveAsPrefabAsset(root,EffectPath);Object.DestroyImmediate(root);
    }
    static void UpgradeWorld(string path,bool planted)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var old=root.transform.Find("Spike Art");if(old)Object.DestroyImmediate(old.gameObject);
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),root.transform);art.name="Spike Art";
            // Compensate the legacy prefab scale without changing its trigger radius.
            var scale=root.transform.lossyScale;art.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            if(planted)
            {
                root.GetComponent<Spike>().explosionTime=45;
                var p=root.GetComponent<SpikePresentation>();if(!p)p=root.AddComponent<SpikePresentation>();p.device=art.GetComponent<SpikeDeviceVisual>();
                p.source=root.GetComponent<AudioSource>();if(!p.source)p.source=Audio(root);p.source.playOnAwake=false;p.source.spatialBlend=1;p.source.minDistance=2;p.source.maxDistance=35;p.source.rolloffMode=AudioRolloffMode.Linear;
                p.armSound=armed;p.beepSound=beep;p.defuseSound=defuse;p.halfSound=half;p.safeSound=safe;p.detonationPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(EffectPath).GetComponent<SpikeDetonation>();
                var oldRing=root.transform.Find("Pulse wave");if(oldRing)Object.DestroyImmediate(oldRing.gameObject);
                var wave=Part("Pulse wave",root.transform,new(0,.025f,0),Vector3.one,fx,ring);p.pulseRing=wave.transform;p.ringRenderer=wave.GetComponent<Renderer>();wave.SetActive(false);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Layer(GameObject g,int layer){foreach(var t in g.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}
    static void AuthorCharacter(string path,string character)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var camera=root.GetComponentInChildren<Camera>(true).transform;
            var fp=root.transform.Find("CameraRoot/Controller Model_Riging (1)");
            if(!fp) throw new Exception(character+" has no first person rig");
            var tp=root.transform.Find("Model");
            if(!tp)throw new Exception(character+" has no world rig");
            var presentation=root.GetComponent<SpikeInteractionPresentation>();if(!presentation)presentation=root.AddComponent<SpikeInteractionPresentation>();
            presentation.firstPerson=BuildRig(fp,camera,true,character);
            presentation.thirdPerson=BuildRig(tp,root.transform,false,character);
            if(!presentation.source)presentation.source=Audio(root);presentation.equipSound=equip;presentation.inputSound=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Contact.wav");
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void RebuildFirstPersonRigs()
    {
        if(!Application.isBatchMode)throw new Exception("Use batch with the project closed");
        shell=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Ceramic.mat");
        dark=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Graphite.mat");
        steel=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Titanium.mat");
        light=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/CoreCyan.mat");
        glass=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Display.mat");
        bevel=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Bevel.asset");
        cylinder=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Prism.asset");
        foreach(var role in Characters)
        {
            string path=$"Assets/ProjectVector/Shared/Characters/Prefabs/{role}/{role}.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var fp=root.transform.Find("CameraRoot/Controller Model_Riging (1)");
                root.GetComponent<SpikeInteractionPresentation>().firstPerson=BuildRig(fp,root.GetComponentInChildren<Camera>(true).transform,true,role);
                var transition=root.GetComponent<FirstPersonEquipmentTransition>();
                if(!transition)transition=root.AddComponent<FirstPersonEquipmentTransition>();
                transition.model=fp;
                if(!root.GetComponent<FirstPersonEquipmentPose>())root.AddComponent<FirstPersonEquipmentPose>();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    static SpikeInteractionPresentation.Rig BuildRig(Transform model,Transform frame,bool fp,string character)
    {
        foreach(string name in new[]{"Spike interaction device","Spike defuse probe"}){var old=model.Find(name);if(old)Object.DestroyImmediate(old.gameObject);}
        var device=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),model);device.name="Spike interaction device";
        var tool=new GameObject("Spike defuse probe");tool.transform.SetParent(model,false);
        var inverseScale=new Vector3(1/model.lossyScale.x,1/model.lossyScale.y,1/model.lossyScale.z);
        device.transform.localScale=inverseScale*(fp?.72f:1f);tool.transform.localScale=inverseScale;
        BuildProbeV2(tool.transform);
        int layer=LayerMask.NameToLayer(fp?"FirstPersonArms":"PlayerBody");if(layer<0)layer=0;Layer(device,layer);Layer(tool,layer);
        // First person owns arms only; the world interaction owns a complete grounded pose.
        var skeleton=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.bones).Where(t=>t&&!t.name.StartsWith("Coat_")).Distinct().ToArray();
        var armBones=skeleton.Where(t=>t.name.Contains("Arm")||t.name.Contains("Hand")||t.name.Contains("Finger")||t.name.Contains("Thumb")).ToArray();
        if(armBones.Length<6)throw new Exception("Missing arm bones: "+character);
        var allBones=new HashSet<Transform>(skeleton);
        foreach(var b in skeleton)for(var p=b.parent;p&&p!=model;p=p.parent)allBones.Add(p);
        // The firearm Animator also keys the shoulders/spine. An arm-only sample
        // leaves those parent transforms in the gun pose and detaches both grips.
        var fpBones=new HashSet<Transform>(armBones);
        foreach(var b in armBones)for(var p=b.parent;p&&p!=model;p=p.parent)fpBones.Add(p);
        var bones=(fp?fpBones.AsEnumerable():allBones.AsEnumerable()).Concat(new[]{model,device.transform,tool.transform}).Distinct().ToArray();
        var positions=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();
        Transform Bone(string n)
        {
            string mapped=n.Replace("LeftArm","UpperArmL").Replace("RightArm","UpperArmR").Replace("LeftForeArm","LoweArmL").Replace("RightForeArm","LoweArmR").Replace("LeftHand","HandL").Replace("RightHand","HandR");
            return armBones.First(t=>t.name.EndsWith(n)||t.name==mapped);
        }
        Vector3 InFrame(Vector3 world)=>fp?Quaternion.Inverse(frame.rotation)*(world-frame.position):frame.InverseTransformPoint(world);
        Vector3 FromFrame(Vector3 local)=>fp?frame.position+frame.rotation*local:frame.TransformPoint(local);
        var leftShoulder=InFrame(Bone("LeftArm").position);
        var rightShoulder=InFrame(Bone("RightArm").position);
        float armLength=Vector3.Distance(Bone("LeftArm").position,Bone("LeftForeArm").position)+Vector3.Distance(Bone("LeftForeArm").position,Bone("LeftHand").position);
        float fpDepth=.52f;
        float carryHeight=(leftShoulder.y+rightShoulder.y)*.5f-armLength*.48f;
        void Reset(){for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}}
        var palmFrames=new Dictionary<string,Quaternion>();
        foreach(string side in new[]{"Left","Right"})palmFrames[side]=PalmFrame(Bone(side+"Hand"),frame.up);
        void Reach(string side,Vector3 point,Vector3 fingers,Vector3 palm)
        {
            var upper=Bone(side+"Arm");var fore=Bone(side+"ForeArm");var hand=Bone(side+"Hand");
            Vector3 pole=side=="Left"?new Vector3(-.8f,-1,.1f):new Vector3(.8f,-1,.1f);
            upper.rotation=Quaternion.FromToRotation(fore.position-upper.position,frame.TransformDirection(pole))*upper.rotation;
            SkillArmPose.Reach(upper,fore,hand,frame,frame.InverseTransformPoint(point),1);
            hand.rotation=Quaternion.LookRotation(fingers.normalized,Vector3.ProjectOnPlane(palm,fingers).normalized)*Quaternion.Inverse(palmFrames[side]);
        }
        void Curl(string side,float amount)
        {
            var hand=Bone(side+"Hand");
            var basis=hand.rotation*palmFrames[side];
            var axis=Vector3.Cross(basis*Vector3.forward,basis*Vector3.up).normalized;
            foreach(var b in hand.GetComponentsInChildren<Transform>())
            {
                if(!armBones.Contains(b)||b==hand||b.name.Contains("end"))continue;
                string name=b.name;
                if(name.Contains("Thumb"))
                {if(name.EndsWith("1")||name.EndsWith("1R"))b.rotation=Quaternion.AngleAxis(amount*.3f,axis)*b.rotation;continue;}
                if(!(name.Contains("Index")||name.Contains("Middle")||name.Contains("Ring")||name.Contains("Pinky")||name.Contains("Fingers")))continue;
                bool first=name.EndsWith("1")||name.EndsWith("1L")||name.EndsWith("1R");
                b.rotation=Quaternion.AngleAxis(amount*(first?.35f:1),axis)*b.rotation;
            }
        }
        var restFeet=skeleton.Where(b=>b.name.EndsWith("Foot")||b.name=="FootL"||b.name=="FootR").ToArray();
        var leftFoot=restFeet.FirstOrDefault(b=>b.name.Contains("Left")||b.name=="FootL");
        var rightFoot=restFeet.FirstOrDefault(b=>b.name.Contains("Right")||b.name=="FootR");
        var pelvis=leftFoot?leftFoot.parent.parent.parent:null;
        var rearFootOffsets=new List<Vector3>();
        if(!fp&&rightFoot)foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var vertices=renderer.sharedMesh.vertices;var weights=renderer.sharedMesh.boneWeights;
            if(weights.Length!=vertices.Length)continue;
            var skinBones=renderer.bones;var bind=renderer.sharedMesh.bindposes;
            var matrices=skinBones.Select((b,i)=>b.localToWorldMatrix*bind[i]).ToArray();
            var footBones=skinBones.Select(b=>b&&b.IsChildOf(rightFoot)).ToArray();
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];float strength=0;
                if(w.weight0>0&&footBones[w.boneIndex0])strength+=w.weight0;
                if(w.weight1>0&&footBones[w.boneIndex1])strength+=w.weight1;
                if(w.weight2>0&&footBones[w.boneIndex2])strength+=w.weight2;
                if(w.weight3>0&&footBones[w.boneIndex3])strength+=w.weight3;
                if(strength>.5f)
                {
                    var vertex=vertices[i];
                    var world=matrices[w.boneIndex0].MultiplyPoint3x4(vertex)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(vertex)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(vertex)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(vertex)*w.weight3;
                    rearFootOffsets.Add(world-rightFoot.position);
                }
            }
        }
        if(!fp)Debug.Log("REAR_FOOT_SHAPE "+character+" vertices="+rearFootOffsets.Count+" range="+(rearFootOffsets.Count==0?"none":rearFootOffsets.Min(p=>p.y)+" / "+rearFootOffsets.Max(p=>p.y)));
        float feetY=restFeet.Length>0?restFeet.Min(b=>b.position.y):frame.position.y;
        if(!fp)Debug.Log("DEVICE_FOOT "+character+" y="+feetY+" root="+frame.position.y+" names="+string.Join(",",restFeet.Select(b=>b.name)));
        SpikeInteractionPresentation.PoseFrame[] plantFrames=null,defuseFrames=null;
        AnimationClip Clip(bool planting)
        {
            float[] times=planting?new[]{0f,.18f,.42f,.65f,.80f,.92f,1.04f,1.16f,1.28f,1.40f,1.52f,1.64f,1.78f,1.95f,2.15f,2.4f,2.7f,2.9f,3f}:new[]{0f,.2f,.4f,.65f,1.2f,2.4f,2.5f,2.6f,3.2f,4.3f,4.75f,5f};
            if(!fp)times=times.Concat(Enumerable.Range(0,(planting?90:150)+1).Select(i=>i/30f)).Distinct().OrderBy(t=>t).ToArray();
            var qs=new Quaternion[times.Length][];var ps=new Vector3[times.Length][];
            for(int s=0;s<times.Length;s++)
            {
                Reset();float time=times[s];float lift=Mathf.SmoothStep(0,1,time/.55f);float lower=planting?Mathf.SmoothStep(0,1,(time-1.95f)/.82f):0;float kneeCorrection=0;
                if(fp)
                {
                    // Center the whole viewmodel; preserve the character's shoulder spacing
                    // and bone lengths rather than stretching its left arm across a gun pose.
                    float center=(leftShoulder.x+rightShoulder.x)*.5f;
                    model.position+=frame.rotation*new Vector3(-center,-.27f-(leftShoulder.y+rightShoulder.y)*.5f,.25f);
                }
                else if(pelvis&&leftFoot&&rightFoot)
                {
                    var footPositions=restFeet.Select(b=>b.position).ToArray();var footRotations=restFeet.Select(b=>b.rotation).ToArray();
                    float kneel=Mathf.SmoothStep(0,1,planting?(time-.3f)/1.1f:time/.8f);
                    float hipHeight=character=="Controller"?.40f:character=="Entry"?.39f:.44f;
                    var thighCenter=(leftFoot.parent.parent.position+rightFoot.parent.parent.position)*.5f;
                    var targetHip=frame.position+frame.forward*.18f+Vector3.up*(feetY-frame.position.y+hipHeight-.115f);
                    // Sentinel's two thighs are parented to Armature rather than a Hips bone.
                    // Position the joint midpoint, never assume the parent origin is the pelvis.
                    pelvis.position+=(targetHip-thighCenter)*kneel;
                    for(int pass=0;pass<2;pass++)
                    {
                      for(int f=0;f<restFeet.Length;f++)
                      {
                        var foot=restFeet[f];bool left=foot==leftFoot;
                        var knee=foot.parent;var thigh=knee.parent;
                        float rearHeight=rearFootOffsets.Count==0?feetY-frame.position.y:.008f-rearFootOffsets.Min(p=>(Quaternion.AngleAxis(135*kneel,frame.right)*p).y);
                        var footTarget=frame.position+frame.right*(left?-.14f:.14f)+frame.forward*(left?.34f:-.22f)+Vector3.up*(left?feetY-frame.position.y:rearHeight);
                        footTarget=Vector3.Lerp(footPositions[f],footTarget,kneel)+Vector3.up*(Mathf.Sin(kneel*Mathf.PI)*.045f);
                        thigh.rotation=Quaternion.FromToRotation(knee.position-thigh.position,frame.forward-Vector3.up*.5f)*thigh.rotation;
                        SkillArmPose.Reach(thigh,knee,foot,frame,frame.InverseTransformPoint(footTarget),1);
                        // The rear shin lies back along the floor. Extend the ankle
                        // so the toes point back and the instep rests down, instead
                        // of keeping the standing foot forward and folding it backward.
                        foot.rotation=Quaternion.AngleAxis(left?0:135*kneel,frame.right)*footRotations[f];
                      }
                      if(pass==0)
                      {
                          kneeCorrection=Mathf.Max(0,frame.position.y+.055f-rightFoot.parent.position.y)*kneel;
                          pelvis.position+=Vector3.up*kneeCorrection+frame.forward*kneeCorrection*.5f;
                      }
                    }
                }
                if(!fp)
                {
                    var spine=skeleton.FirstOrDefault(b=>b.name=="Spine"||b.name.EndsWith(":Spine")||b.name.EndsWith("_Spine"));
                    float kneel=Mathf.SmoothStep(0,1,planting?(time-.3f)/1.1f:time/.8f);
                    float lean=(planting?22*kneel+23*lower:32*kneel)+Mathf.Min(10,kneeCorrection*150);
                    if(spine)spine.rotation=Quaternion.AngleAxis(lean,frame.right)*spine.rotation;
                    var head=skeleton.FirstOrDefault(b=>b.name=="Head"||b.name.EndsWith(":Head")||b.name.EndsWith("_Head"));
                    if(head)head.rotation=Quaternion.AngleAxis(-lean*.35f,frame.right)*head.rotation;
                }
                float worldY=(Bone("LeftArm").position.y+Bone("RightArm").position.y)*.5f-frame.position.y-armLength*.5f;
                Vector3 basePosition=fp?new Vector3(0,Mathf.Lerp(-.66f,-.24f,lift)-lower*.30f,fpDepth-lower*.04f):new Vector3(0,0,.43f);
                var shoulderCenter=(Bone("LeftArm").position+Bone("RightArm").position)*.5f;
                var heldPosition=shoulderCenter+frame.forward*.25f-Vector3.up*.27f;
                var placedPosition=frame.position+frame.forward*PlayerSpike.PlantForwardOffset+Vector3.up*.012f;
                device.transform.position=fp?FromFrame(basePosition):Vector3.Lerp(heldPosition,placedPosition,lower);
                // Lean the upper core away from the face while it is being carried.
                // The footprint reaches the floor upright only at the end.
                device.transform.rotation=frame.rotation*Quaternion.Euler(fp?-24+lower*38:Mathf.Lerp(50,0,lower),fp?-12:0,fp?7:0);
                tool.transform.position=fp?FromFrame(new Vector3(.075f,Mathf.Lerp(-.6f,-.25f,lift),fpDepth-.07f)):frame.position+frame.forward*.42f+frame.right*.08f+Vector3.up*Mathf.Max(.26f,worldY);
                tool.transform.rotation=frame.rotation*Quaternion.Euler(fp?12:35,fp?-8:0,-8);
                if(planting)
                {
                    // Both palms carry the exposed side grips. As the device is
                    // lowered, the palms settle on its top before releasing it.
                    float release=Mathf.SmoothStep(0,1,(time-2.8f)/.2f);
                    foreach(string side in new[]{"Left","Right"})
                    {
                        float sign=side=="Left"?-1:1;
                        var grip=Vector3.Lerp(new Vector3(sign*.17f,.065f,-.02f),new Vector3(sign*.07f,.335f,-.07f),lower);
                        grip+=new Vector3(sign*.045f,.04f,-.045f)*release;
                        var orientation=Quaternion.Slerp(Quaternion.LookRotation(Vector3.up,Vector3.left*sign),Quaternion.LookRotation(Vector3.forward,Vector3.down),lower);
                        Reach(side,device.transform.TransformPoint(grip),device.transform.rotation*(orientation*Vector3.forward),device.transform.rotation*(orientation*Vector3.up));
                        Curl(side,Mathf.Lerp(38,10,lower)*(1-release));
                    }
                }
                else
                {
                    var rightGrip=new Vector3(.06f,-.025f,-.06f);
                    Reach("Right",tool.transform.TransformPoint(rightGrip),tool.transform.forward, -tool.transform.right);
                    tool.transform.position=Bone("RightHand").position-tool.transform.TransformVector(rightGrip);
                    Reach("Left",tool.transform.TransformPoint(new Vector3(-.055f,-.035f,-.015f)),tool.transform.forward,tool.transform.up);
                    Curl("Left",28);Curl("Right",35);
                }
                qs[s]=bones.Select(t=>t.localRotation).ToArray();ps[s]=bones.Select(t=>t.localPosition).ToArray();
                if(!fp&&planting&&time>=2.9f)Debug.Log("DEVICE_PLACE_CONTACT "+character+" shoulder="+Bone("LeftArm").position+" hand="+Bone("LeftHand").position+" device="+device.transform.position+" armLength="+armLength);
            }
            var samples=times.Select((time,i)=>new SpikeInteractionPresentation.PoseFrame{time=time,positions=ps[i],rotations=qs[i]}).ToArray();
            if(planting)plantFrames=samples;else defuseFrames=samples;
            Reset();var clip=new AnimationClip{name=character+(fp?"_FP":"_TP")+(planting?"_Plant":"_Defuse"),frameRate=60,wrapMode=WrapMode.ClampForever};
            for(int i=0;i<bones.Length;i++)
            {
                string binding=AnimationUtility.CalculateTransformPath(bones[i],model);
                for(int a=0;a<4;a++){var c=new AnimationCurve();for(int s=0;s<times.Length;s++)c.AddKey(times[s],qs[s][i][a]);clip.SetCurve(binding,typeof(Transform),"localRotation."+"xyzw"[a],c);}
                for(int a=0;a<3;a++){var c=new AnimationCurve();for(int s=0;s<times.Length;s++)c.AddKey(times[s],ps[s][i][a]);clip.SetCurve(binding,typeof(Transform),"localPosition."+"xyz"[a],c);}
            }
            clip.EnsureQuaternionContinuity();return Save(clip,"Animations/"+clip.name+".anim");
        }
        var plant=Clip(true);var release=Clip(false);Reset();device.SetActive(false);tool.SetActive(false);
        Debug.Log("SPIKE_RIG "+character+" fp="+fp+" shoulder="+frame.InverseTransformPoint(Bone("LeftArm").position)+" bones="+bones.Length);
        return new SpikeInteractionPresentation.Rig{model=model,bones=bones,plant=plant,defuse=release,plantFrames=plantFrames,defuseFrames=defuseFrames,device=device,tool=tool};
    }
    public static void Verify()
    {
        foreach(var c in Characters)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Characters/Prefabs/"+c+"/"+c+".prefab");var p=prefab.GetComponent<SpikeInteractionPresentation>();
            if(!p||!p.firstPerson.plant||!p.thirdPerson.defuse||p.firstPerson.device.activeSelf||p.thirdPerson.tool.activeSelf)throw new Exception("Spike bindings invalid: "+c);
            if(Mathf.Abs(p.firstPerson.plant.length-3)>.001f||Mathf.Abs(p.firstPerson.defuse.length-5)>.001f)throw new Exception("Gameplay duration changed");
            foreach(var rig in new[]{p.firstPerson,p.thirdPerson})if(rig.bones.Any(t=>!t))throw new Exception("Missing pose binding");
        }
        var spike=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Modules/Match/Resources/Prefabs/Spike/Spike.prefab");
        if(!spike.GetComponent<SpikePresentation>().detonationPrefab)throw new Exception("Missing detonation");
        VerifyRestore();
        Debug.Log("SPIKE_VERIFY_PASS: 4 characters, FP/TP bindings, 3s/5s clips, device, effect");
    }
}
#endif
