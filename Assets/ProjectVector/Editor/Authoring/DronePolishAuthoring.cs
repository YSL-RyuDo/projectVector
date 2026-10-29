#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

public static class DronePolishAuthoring
{
    const string Dir="Assets/Audio/Drone";
    const string Scene="Assets/Scenes/Game_TestVersion.unity";
    static readonly Color Ink=new Color(.025f,.05f,.065f,.83f), Cyan=new Color(.35f,.91f,1), White=new Color(.87f,.94f,.95f);
    static TMP_FontAsset font;
    static T Component<T>(GameObject go) where T:UnityEngine.Component {var c=go.GetComponent<T>();return c?c:go.AddComponent<T>();}
    static void Ref(Object owner,string field,Object value) { var s=new SerializedObject(owner); s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
    static void Refs(Object owner,string field,Object[] values) {var s=new SerializedObject(owner);var p=s.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();}
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    { var g=new GameObject(name,typeof(RectTransform));var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r; }
    static Image Image(Transform p,string name,float x,float y,float w,float h,Color c)
    {var r=Rect(p,name,x,y,w,h);var i=r.gameObject.AddComponent<Image>();i.color=c;i.raycastTarget=false;return i;}
    static TMP_Text Text(Transform p,string name,string text,float x,float y,float w,float h,int size,Color c)
    {var r=Rect(p,name,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=c;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.NoWrap;return t;}
    static AudioClip Sound(string name,int kind)
    {
        Directory.CreateDirectory(Dir);
        string path=$"{Dir}/{name}.wav";
        int rate=22050, count=(int)(rate*(kind==0?1f:.28f));
        using(var writer=new BinaryWriter(File.Create(path))) {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            for(int n=0;n<count;n++) {
                double t=(double)n/rate, envelope=kind==0?1:Math.Sin(Math.PI*n/count)*Math.Exp(-t*7);
                double hz=kind==0?120:kind==1?1100:kind==2?190:440;
                double sample=(Math.Sin(2*Math.PI*hz*t)+.3*Math.Sin(2*Math.PI*hz*2*t))*envelope*.22;
                writer.Write((short)(sample*short.MaxValue));
            }
        }
        AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Run with Unity closed in batch mode");
        var motor=Sound("Rotor",0);var hit=Sound("Confirmed",1);var damage=Sound("Interrupted",2);var ready=Sound("Unfold",3);
        AuthorDrone(motor);
        AuthorArm();
        EditorSceneManager.OpenScene(Scene);
        var hud=Object.FindFirstObjectByType<DroneHUDUI>(FindObjectsInactive.Include);
        var old=new SerializedObject(hud);
        var root=(GameObject)old.FindProperty("_root").objectReferenceValue;
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/NanumGothic SDF.asset");
        if(hud.gameObject==root) {
            var host=new GameObject("DroneHUDController");host.transform.SetParent(root.transform.parent.parent,false);
            Object.DestroyImmediate(hud);hud=host.AddComponent<DroneHUDUI>();
        }
        for(int i=root.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        var content=Rect(root.transform,"FlightInstruments",0,0,1920,1080);
        var fade=Rect(content,"LowerEdgeReadability",0,-415,1920,250).gameObject.AddComponent<DroneHudFadeGraphic>();
        fade.color=new Color(.015f,.025f,.035f,.68f);fade.raycastTarget=false;
        // Sparse optical framing, no diagnostic panels over the game world.
        foreach(int side in new[]{-1,1})foreach(int vertical in new[]{-1,1}) {
            Image(content,"OpticalCorner",side*865,vertical*421,28,1,new Color(.8f,1,1,.4f));
            Image(content,"OpticalEdge",side*879,vertical*410,1,23,new Color(.8f,1,1,.4f));
        }
        var droneIcon=Image(content,"DroneSkillIcon",0,-360,47,47,White);
        droneIcon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Character/Initiator/Initiator_Q.png");droneIcon.preserveAspect=true;
        var hp=Text(content,"Integrity","100",-145,-369,75,40,32,White);
        hp.fontStyle=FontStyles.Bold;
        Text(content,"IntegrityLabel","내구도",-145,-402,80,24,15,new Color(.86f,.9f,.9f,.8f));
        Image(content,"IntegrityTrack",0,-407,170,5,new Color(.05f,.08f,.1f,.6f));
        var fill=Image(content,"IntegrityFill",0,-407,170,5,Cyan);
        ConfigureIntegrityFill(fill);
        for(int i=1;i<10;i++)Image(content,"IntegrityDivision",-85+i*17,-407,2,5,new Color(.07f,.12f,.13f,.8f));
        var taser=Text(content,"Charge","1 / 1",145,-369,100,40,27,White);
        taser.fontStyle=FontStyles.Bold;
        Text(content,"ChargeLabel","테이저",145,-402,100,24,15,new Color(.86f,.9f,.9f,.8f));
        // Altitude ticks sit off the aiming axis, with only the current height shown.
        for(int i=0;i<9;i++)Image(content,"AltitudeTick",190+(i%4==0?3:0),-72+i*18,i%4==0?12:6,1,new Color(.8f,1,1,.45f));
        var telemetry=Text(content,"Altitude","2.4m",223,-96,90,24,16,White);
        Image(content,"AimDot",0,0,3,3,White);
        var reticle=Rect(content,"OpticalReticle",0,0,64,64).gameObject.AddComponent<DroneReticleGraphic>();reticle.color=new Color(.57f,1f,.75f,.85f);reticle.raycastTarget=false;
        var flash=Image(content,"FirePulse",0,-31,54,2,new Color(0,1,1,0));
        var marker=Rect(content,"ServerConfirmedHit",0,0,80,80);
        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1}) {
            var line=Image(marker,"HitStroke",x*12,y*12,13,3,Cyan);
            line.rectTransform.localEulerAngles=new Vector3(0,0,x*y*45);
        }
        marker.gameObject.SetActive(false);
        var feedback=Text(root.transform,"Feedback","",0,-63,600,34,22,Cyan);
        var mode=Text(root.transform,"ConnectionState","드론 조종",0,365,600,30,18,White);
        var controls=Text(root.transform,"Controls","SPACE  상승     CTRL  하강     좌클릭  발사     Q  복귀",0,-463,1100,28,17,White);
        var edges=new[]{Image(content,"DamageTop",0,531,1920,6,Color.clear),Image(content,"DamageBottom",0,-531,1920,6,Color.clear),Image(content,"DamageLeft",-951,0,6,1080,Color.clear),Image(content,"DamageRight",951,0,6,1080,Color.clear)};
        var audio=Component<AudioSource>(hud.gameObject);audio.playOnAwake=false;audio.spatialBlend=0;
        Ref(hud,"_root",root);Ref(hud,"_flightInstruments",content.gameObject);Ref(hud,"_taserStatusText",taser);Ref(hud,"_droneHpText",hp);Ref(hud,"_controlGuideText",controls);
        Ref(hud,"_telemetryText",telemetry);Ref(hud,"_feedbackText",feedback);Ref(hud,"_modeText",mode);Ref(hud,"_hpFill",fill);Ref(hud,"_hitMarker",marker.gameObject);Ref(hud,"_fireFlashImage",flash);
        Refs(hud,"_damageEdges",edges);Ref(hud,"_audio",audio);Ref(hud,"_hitSound",hit);Ref(hud,"_damageSound",damage);Ref(hud,"_launchSound",ready);
        Ref(hud,"_reticle",reticle);
        foreach(var t in root.GetComponentsInChildren<TMP_Text>(true)) {
            var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.65f);shadow.effectDistance=new Vector2(0,-1);
        }
        root.SetActive(false);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();Debug.Log("DRONE_AUTHORING_PASS");
    }
    static void ConfigureIntegrityFill(Image fill)
    {
        // A plain quad has no rounded sprite borders to compress into pointed ends.
        fill.sprite=null;
        fill.type=UnityEngine.UI.Image.Type.Simple;
        var rect=fill.rectTransform;
        var position=rect.anchoredPosition;
        position.x-=rect.rect.width*rect.pivot.x;
        rect.pivot=new Vector2(0,.5f);
        rect.anchoredPosition=position;
        rect.localScale=Vector3.one;
    }
    public static void FixIntegrityEdges()
    {
        if(!Application.isBatchMode)throw new Exception("Run with Unity closed in batch mode");
        EditorSceneManager.OpenScene(Scene);
        var hud=Object.FindFirstObjectByType<DroneHUDUI>(FindObjectsInactive.Include);
        var fill=(Image)new SerializedObject(hud).FindProperty("_hpFill").objectReferenceValue;
        ConfigureIntegrityFill(fill);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        hud.SetDroneHp(57,100);
        if(fill.sprite || fill.type!=UnityEngine.UI.Image.Type.Simple ||
           Mathf.Abs(fill.rectTransform.localScale.x-.57f)>.001f || fill.rectTransform.pivot.x!=0)
            throw new Exception("Straight integrity fill failed at partial health");
        hud.SetDroneHp(0,100);
        if(fill.rectTransform.localScale.x!=0)throw new Exception("Empty integrity fill remains visible");
        Debug.Log("DRONE_STRAIGHT_INTEGRITY_PASS");
        DronePolishVerification.Capture();
    }
    static void AuthorDrone(AudioClip motor)
    {
        string path="Assets/Prefab/Character/Initiator/Drone.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            root.transform.localScale=Vector3.one;
            var visual=root.transform.Find("drone");visual.localScale=Vector3.one*.28f;
            root.GetComponent<SphereCollider>().radius=.48f;
            var rb=root.GetComponent<Rigidbody>();rb.useGravity=false;rb.constraints=RigidbodyConstraints.FreezeRotation;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.interpolation=RigidbodyInterpolation.Interpolate;
            var camera=root.GetComponentInChildren<Camera>(true);camera.transform.localPosition=new Vector3(0,.04f,.4f);camera.nearClipPlane=.03f;
            var presentation=Component<DronePresentation>(root);
            Ref(presentation,"_visual",visual);Refs(presentation,"_fans",visual.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("fan.")).ToArray());
            var source=Component<AudioSource>(root);source.playOnAwake=false;source.spatialBlend=1;source.minDistance=2;source.maxDistance=25;
            var rotor=root.transform.Find("RotorAudio");if(!rotor){rotor=new GameObject("RotorAudio").transform;rotor.SetParent(root.transform,false);}
            var loop=Component<AudioSource>(rotor.gameObject);loop.clip=motor;loop.loop=true;loop.playOnAwake=true;loop.volume=.08f;loop.spatialBlend=1;loop.minDistance=2;loop.maxDistance=18;Ref(presentation,"_motor",loop);
            var gun=root.GetComponent<TaserGun>();Ref(gun,"_audioSource",source);Ref(gun,"_fireSound",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/KSW/Scifi Guns SFX Pack/Gun2_1.wav"));
            var spark=root.transform.Find("MuzzleSpark").GetComponent<ParticleSystem>();spark.transform.localPosition=new Vector3(0,.02f,.48f);
            ConfigureSpark(spark,24,.2f);Ref(gun,"_muzzleSpark",spark);
            var impact=root.transform.Find("ImpactSpark");if(!impact){impact=new GameObject("ImpactSpark").transform;impact.SetParent(root.transform,false);}
            var particles=Component<ParticleSystem>(impact.gameObject);ConfigureSpark(particles,18,.3f);
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=spark.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            Ref(gun,"_impactSpark",particles);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }
    static void ConfigureSpark(ParticleSystem p,int count,float duration)
    {
        p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=p.main;m.loop=false;m.playOnAwake=false;m.duration=duration;m.startLifetime=new ParticleSystem.MinMaxCurve(.06f,.18f);m.startSpeed=new ParticleSystem.MinMaxCurve(1,3);m.startSize=new ParticleSystem.MinMaxCurve(.015f,.04f);m.startColor=Cyan;m.simulationSpace=ParticleSystemSimulationSpace.World;
        var e=p.emission;e.rateOverTime=0;e.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});var sh=p.shape;sh.shapeType=ParticleSystemShapeType.Sphere;sh.radius=.025f;
    }
    static void AuthorArm()
    {
        string path="Assets/Prefab/Character/Initiator/Initiator.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var rig=Component<DroneLaunchRig>(root);
            var fore=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:LeftForeArm");
            var upper=fore.parent;
            var device=fore.Find("DroneLaunchDevice");
            if(!device) {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KSW/Drone/prefab/drone.prefab"),fore);
                go.name="DroneLaunchDevice";device=go.transform;
            }
            device.localPosition=new Vector3(-.12f,0,.08f);device.localRotation=Quaternion.Euler(0,0,90);device.localScale=Vector3.one*.065f;
            Ref(rig,"_device",device.gameObject);Ref(rig,"_upperArm",upper);Ref(rig,"_forearm",fore);
            Refs(rig,"_fans",device.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("fan.")).ToArray());device.gameObject.SetActive(false);
            Ref(root.GetComponent<InitiatorSkill_Q>(),"_launchRig",rig);
            var spawn=root.transform.Find("DroneLaunchPoint");if(!spawn){spawn=new GameObject("DroneLaunchPoint").transform;spawn.SetParent(root.transform,false);}
            spawn.localPosition=new Vector3(-.4f,.3f,1.1f);Ref(root.GetComponent<InitiatorSkill_Q>(),"_droneSpawnPoint",spawn);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
