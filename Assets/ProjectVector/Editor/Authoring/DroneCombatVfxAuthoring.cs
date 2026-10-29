#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DroneCombatVfxAuthoring
{
    const string Path = "Assets/Prefab/Character/Initiator/Drone.prefab";
    const string Output = "C:/Users/GCT/Downloads/Vector_Drone_20261021/CombatFX";
    static readonly Color Cyan = new Color(.15f,.78f,1);
    static void Ref(Object o,string name,Object value) { var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
    static Transform Child(Transform parent,string name) { var t=parent.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(parent,false);}return t; }
    static T Component<T>(Transform t) where T:Component { var c=t.GetComponent<T>();return c?c:t.gameObject.AddComponent<T>(); }
    static Material Material(string name,bool radial)
    {
        string path="Assets/Materials/Drone/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Vector/DroneEnergy"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetFloat("_Radial",radial?1:0);EditorUtility.SetDirty(mat);return mat;
    }
    static LineRenderer Line(Transform parent,string name,Material mat,bool ring=false)
    {
        var l=Component<LineRenderer>(Child(parent,name));l.sharedMaterial=mat;l.enabled=false;l.useWorldSpace=!ring;l.loop=ring;
        l.widthCurve=AnimationCurve.Constant(0,1,1);l.numCapVertices=4;l.numCornerVertices=2;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;
        l.textureMode=LineTextureMode.Stretch;return l;
    }
    static ParticleSystem Burst(Transform parent,string name,Material mat,int count,float speed,float size,float life,Color color,bool streak=false)
    {
        var p=Component<ParticleSystem>(Child(parent,name));p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=p.main;m.playOnAwake=false;m.loop=false;m.duration=1;m.startLifetime=new ParticleSystem.MinMaxCurve(life*.5f,life);m.startSpeed=new ParticleSystem.MinMaxCurve(speed*.4f,speed);m.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);m.startColor=color;m.simulationSpace=ParticleSystemSimulationSpace.World;m.maxParticles=96;
        m.gravityModifier=streak?.35f:0;
        var e=p.emission;e.rateOverTime=0;e.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
        var sh=p.shape;sh.enabled=true;sh.shapeType=ParticleSystemShapeType.Sphere;sh.radius=.035f;
        var c=p.colorOverLifetime;c.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(color,.18f),new GradientColorKey(color,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.25f),new GradientAlphaKey(0,1)});c.color=g;
        var s=p.sizeOverLifetime;s.enabled=true;s.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(1,0)));
        var r=p.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=mat;r.renderMode=streak?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;r.lengthScale=streak?2:1;r.velocityScale=streak?.02f:0;r.maxParticleSize=.3f;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        p.useAutoRandomSeed=false;p.randomSeed=(uint)(name.Length*13+count);return p;
    }
    public static void Run()
    {
        var soft=Material("EnergyParticle",true);var beam=Material("EnergyBeam",false);
        var root=PrefabUtility.LoadPrefabContents(Path);
        try {
            var fx=Component<DroneCombatVfx>(root.transform);var group=Child(root.transform,"CombatFX");
            Ref(fx,"_core",Line(group,"DischargeCore",beam));Ref(fx,"_halo",Line(group,"DischargeGlow",beam));Ref(fx,"_arc",Line(group,"DischargeArc",beam));Ref(fx,"_impactRing",Line(group,"ConfirmedImpactRing",beam,true));
            var muzzle=Burst(group,"MuzzleDischarge",soft,12,2.4f,.08f,.14f,Cyan,true);
            var shape=muzzle.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=22;
            Burst(muzzle.transform,"Flash",soft,1,0,.42f,.07f,new Color(.55f,.9f,1));
            var impact=Burst(group,"ConfirmedSparks",soft,22,3.8f,.045f,.38f,Cyan,true);
            var flash=Burst(group,"ConfirmedFlash",soft,2,0,.65f,.13f,new Color(.5f,.9f,1));
            Ref(fx,"_muzzle",muzzle);Ref(fx,"_impact",impact);Ref(fx,"_flash",flash);
            var breakup=Child(root.transform,"BreakupFX");Ref(fx,"_breakup",breakup);
            Burst(breakup,"ElectricalFlash",soft,2,0,.85f,.14f,new Color(.4f,.85f,1));
            Burst(breakup,"HotFragments",soft,32,4.5f,.055f,.7f,new Color(1,.53f,.18f),true);
            Burst(breakup,"ResidualCharge",soft,16,1.7f,.07f,.38f,Cyan,true);
            var audio=Component<AudioSource>(Child(group,"ImpactAudio"));audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=2;audio.maxDistance=22;audio.volume=.32f;audio.pitch=1.4f;
            audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/KSW/Scifi Guns SFX Pack/Gun2_1.wav");Ref(fx,"_impactAudio",audio);
            var breaking=Component<AudioSource>(breakup);EditorUtility.CopySerialized(audio,breaking);breaking.pitch=.65f;breaking.volume=.45f;
            Ref(root.GetComponent<TaserGun>(),"_combatVfx",fx);
            PrefabUtility.SaveAsPrefabAsset(root,Path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        // Replace the old square particle material on player shock receivers, too.
        foreach(string name in new[]{"Initiator","Controller","Entry","Sentinel"}) {
            string path=$"Assets/Prefab/Character/{name}/{name}.prefab";var actor=PrefabUtility.LoadPrefabContents(path);
            try { var p=actor.transform.Find("ElectricShockSparks");if(p)p.GetComponent<ParticleSystemRenderer>().sharedMaterial=soft;PrefabUtility.SaveAsPrefabAsset(actor,path); }
            finally{PrefabUtility.UnloadPrefabContents(actor);}
        }
        AssetDatabase.SaveAssets();
        SkillVfxClarityAuthoring.Run();
        Capture();
        DroneVisualReview.PoseReview();
        // Cursor regression needs a separate -nographics batch, not this render batch.
        Debug.Log("DRONE_COMBAT_VFX_PASS");
    }
    public static void Capture()
    {
        Directory.CreateDirectory(Output+"/frames");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/TestMap.prefab"));map.transform.position=Vector3.zero;
        var info=map.GetComponent<MapInfo>();if(info.barriers)info.barriers.SetActive(false);
        var camera=new GameObject("VFXReviewCamera").AddComponent<Camera>();camera.fieldOfView=48;camera.nearClipPlane=.03f;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.36f,.43f);
        camera.transform.position=info.GetSpawnPoint(0,0).position+Vector3.up*2;
        var forward=info.GetSpawnPoint(1,0).position-info.GetSpawnPoint(0,0).position;forward.y=0;camera.transform.rotation=Quaternion.LookRotation(forward);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.white*.65f;
        var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-35,0);
        var drone=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
        PrefabUtility.UnpackPrefabInstance(drone,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        drone.transform.position=camera.transform.TransformPoint(new Vector3(-1.3f,-.15f,4));
        foreach(var c in drone.GetComponentsInChildren<Camera>(true))c.enabled=false;
        var target=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Sentinel/Sentinel.prefab"));
        foreach(Transform child in target.transform) if(child.name!="Model")child.gameObject.SetActive(false);
        var rootRenderer=target.GetComponent<Renderer>();if(rootRenderer)rootRenderer.enabled=false;
        target.transform.position=camera.transform.TransformPoint(new Vector3(1.2f,-2,7));target.transform.rotation=Quaternion.LookRotation(-forward);
        var animator=target.transform.Find("Model").GetComponentInChildren<Animator>();
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character Animation/Sentinel/Lobby/Sentinel_Lobby_Idle.anim");
        if(animator && clip) { animator.enabled=true;clip.SampleAnimation(animator.gameObject,.3f); }
        var fx=drone.GetComponent<DroneCombatVfx>();var end=target.transform.position+Vector3.up*1.3f;drone.transform.LookAt(end);
        var particles=drone.GetComponentsInChildren<ParticleSystem>(true);foreach(var p in particles)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
        for(int i=0;i<180;i++) {
            if(i==30 || i==75) {
                fx.Fire(drone.transform.position+drone.transform.forward*.45f,end);
                if(!drone.transform.Find("CombatFX/DischargeCore").GetComponent<LineRenderer>().enabled)throw new System.Exception("Discharge did not activate");
                if(drone.transform.Find("CombatFX/ConfirmedImpactRing").GetComponent<LineRenderer>().enabled)throw new System.Exception("Fire predicted an unconfirmed hit");
            }
            if(i==32 || i==77)fx.ConfirmImpact(end,-drone.transform.forward);
            if(i==125){fx.BreakApart();drone.transform.Find("drone").gameObject.SetActive(false);if(drone.transform.Find("BreakupFX"))throw new System.Exception("Breakup would be destroyed with drone");}
            fx.Tick(1f/30);
            foreach(var p in particles)if(p && p.isPlaying) { p.Simulate(1f/30,false,false);p.Play(false); }
            camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
            File.WriteAllBytes(Output+"/frames/"+i.ToString("D4")+".jpg",tex.EncodeToJPG(92));
            if(i==32)File.WriteAllBytes(Output+"/Discharge-Hit.png",tex.EncodeToPNG());
            if(i==128)File.WriteAllBytes(Output+"/Drone-Breakup.png",tex.EncodeToPNG());
        }
        camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
        Debug.Log("DRONE_COMBAT_VFX_CAPTURE_PASS");
    }
}
#endif
