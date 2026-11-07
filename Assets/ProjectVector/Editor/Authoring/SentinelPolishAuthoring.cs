#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class SentinelPolishAuthoring
{
    const string Art="Assets/ProjectVector/Shared/Characters/Effects/Sentinel/Sentinel", Prefabs="Assets/ProjectVector/Shared/Characters/Prefabs/Sentinel/", Hud="Assets/ProjectVector/Shared/Characters/Effects/Initiator/EMP/ElectricStatusHUD.prefab";
    static Material armor,purple,gold,glow;
    static T Ensure<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c?c:go.AddComponent<T>();}
    static void Ref(Object target,string field,Object value)=>ScannerFeedbackAuthoring.Ref(target,field,value);
    static Transform Node(Transform parent,string name){var found=parent.Find(name);if(found)return found;var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static Material Mat(string name,Color color,bool emission=false)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",.45f);m.SetFloat("_Smoothness",.55f);
        if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.7f);}EditorUtility.SetDirty(m);return m;
    }
    static Renderer Shape(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material,Vector3 rotation=default)
    {
        var t=parent.Find(name);if(!t){var go=GameObject.CreatePrimitive(type);t=go.transform;t.SetParent(parent,false);t.name=name;Object.DestroyImmediate(go.GetComponent<Collider>());}
        t.localPosition=pos;t.localScale=scale;t.localRotation=Quaternion.Euler(rotation);var r=t.GetComponent<Renderer>();r.sharedMaterial=material;return r;
    }
    static LineRenderer Ring(Transform parent,string name,float radius,float y,Material material,int count=96,float fraction=1,float width=.025f)
    {
        var t=Node(parent,name);t.localPosition=Vector3.up*y;var line=Ensure<LineRenderer>(t.gameObject);line.useWorldSpace=false;line.loop=fraction>=1;
        line.sharedMaterial=material;line.startWidth=line.endWidth=width;line.numCornerVertices=2;line.positionCount=count;
        for(int i=0;i<count;i++){float a=2*Mathf.PI*i/(count-1)*fraction;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}return line;
    }
    static GameObject Wave()
    {
        var root=new GameObject("SentinelEnergyWave");
        Ring(root.transform,"Equator",.5f,0,glow,96,1,.012f);
        Ring(root.transform,"MeridianA",.5f,0,glow,96,1,.009f).transform.localRotation=Quaternion.Euler(62,0,0);
        Ring(root.transform,"MeridianB",.5f,0,gold,96,1,.009f).transform.localRotation=Quaternion.Euler(-62,30,0);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Art+"/EnergyWave.prefab");Object.DestroyImmediate(root);return prefab;
    }
    static SentinelFieldVfx Field(Transform parent,string name)
    {
        var root=Node(parent,name);var field=Ensure<SentinelFieldVfx>(root.gameObject);
        var boundary=Node(root,"Boundary");Ring(boundary,"Outer",1,.055f,glow,128,1,.035f);Ring(boundary,"InnerTrim",.985f,.06f,gold,96,1,.012f);
        var inner=Node(root,"ExpandingPulse");Ring(inner,"Pulse",1,.075f,glow,96,.92f,.025f);
        var spokes=Node(root,"SegmentedEnergy");
        for(int i=0;i<8;i++){var arc=Ring(spokes,"Arc"+i,.8f,.12f,glow,16,.065f,.035f);arc.transform.localRotation=Quaternion.Euler(0,i*45,0);}
        Ref(field,"boundary",boundary);Ref(field,"inner",inner);Ref(field,"spokes",spokes);return field;
    }
    static AudioSource Sound(Transform parent,string name,string clip,float volume,float pitch)
    {
        var t=Node(parent,name);var s=Ensure<AudioSource>(t.gameObject);s.playOnAwake=false;s.spatialBlend=1;s.volume=volume;s.pitch=pitch;
        s.minDistance=1;s.maxDistance=18;s.rolloffMode=AudioRolloffMode.Linear;s.dopplerLevel=0;
        s.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ProjectVector/Shared/Audio/Drone/"+clip+".wav");return s;
    }
    static void Device(string name,int kind,GameObject wave)
    {
        string path=Prefabs+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{
            // Keep original scripts, colliders, UI and references. Only legacy device meshes are replaced.
            var existing=root.transform.Find("SentinelPresentation");
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))if(!existing || !r.transform.IsChildOf(existing))r.enabled=false;
            var visual=Node(root.transform,"SentinelPresentation");visual.localRotation=kind==2?Quaternion.Euler(90,0,0):Quaternion.identity;
            Shape(visual,"LowerArmor",PrimitiveType.Cylinder,new Vector3(0,.04f,0),new Vector3(.58f,.045f,.58f),armor);
            Shape(visual,"PurpleShell",PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(.49f,.055f,.49f),purple);
            Shape(visual,"TopArmor",PrimitiveType.Cylinder,new Vector3(0,.18f,0),new Vector3(.36f,.018f,.36f),armor);
            var emitter=Node(visual,"RotatingEmitter");
            var lens=Shape(emitter,"EnergyLens",PrimitiveType.Sphere,new Vector3(0,.22f,.05f),new Vector3(.15f,.08f,.15f),glow);
            for(int i=0;i<4;i++){
                float a=i*Mathf.PI*.5f;var position=new Vector3(Mathf.Cos(a)*.23f,.1f,Mathf.Sin(a)*.23f);
                Shape(visual,"ArmorFin"+i,PrimitiveType.Cube,position,new Vector3(.12f,.075f,.17f),armor,new Vector3(0,-i*90,15));
                Shape(visual,"YellowInset"+i,PrimitiveType.Cube,position+Vector3.up*.045f,new Vector3(.07f,.008f,.11f),gold,new Vector3(0,-i*90,0));
            }
            for(int side=-1;side<=1;side+=2){
                Shape(emitter,"Antenna"+side,PrimitiveType.Cylinder,new Vector3(side*.12f,.31f,-.1f),new Vector3(.023f,.11f,.023f),armor,new Vector3(-18,0,-side*17));
                Shape(emitter,"Receiver"+side,PrimitiveType.Sphere,new Vector3(side*.15f,.42f,-.14f),new Vector3(.05f,.08f,.05f),gold);
            }
            if(kind==1){
                // Q is a low trip plate, with wide ground contacts instead of C's tall receivers.
                foreach(var t in emitter.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Antenna")||t.name.StartsWith("Receiver"))t.gameObject.SetActive(false);
                visual.localScale=new Vector3(1.2f,.62f,1.2f);
                for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Shape(visual,"GroundContact"+i,PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*.31f,.025f,Mathf.Sin(a)*.31f),new Vector3(.2f,.035f,.1f),gold,new Vector3(0,-i*120,0));}
            }
            if(kind==2){
                foreach(var t in emitter.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Antenna")||t.name.StartsWith("Receiver"))t.gameObject.SetActive(false);
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Shape(visual,"WallPetal"+i,PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*.25f,.15f,Mathf.Sin(a)*.25f),new Vector3(.21f,.05f,.1f),purple,new Vector3(0,-i*60,18));}
                Ring(emitter,"WallSensorHalo",.21f,.24f,gold,64,.9f,.017f);
            }
            var spiral=Ring(emitter,"SnailCircuit",.12f,.205f,glow,100,1,.013f);spiral.loop=false;
            for(int i=0;i<100;i++){float t=i/99f,a=t*Mathf.PI*5;spiral.SetPosition(i,new Vector3(Mathf.Cos(a)*(.015f+t*.13f),0,Mathf.Sin(a)*(.015f+t*.13f)));}
            var protectedRoot=Node(visual,"OverchargeShield");Ring(protectedRoot,"ProtectionRing",.36f,.18f,gold,64,1,.02f);protectedRoot.gameObject.SetActive(false);
            var fx=Ensure<SentinelDeviceVfx>(root);Ref(fx,"emitter",emitter);Ref(fx,"protection",protectedRoot.gameObject);
            Ref(fx,"detectionAudio",Sound(visual,"DetectionAudio","Interrupted",.3f,1.1f));Ref(fx,"pulseAudio",Sound(visual,"PulseAudio","Confirmed",.5f,.75f));
            var data=new SerializedObject(fx);data.FindProperty("lights").arraySize=1;data.FindProperty("lights").GetArrayElementAtIndex(0).objectReferenceValue=lens;data.ApplyModifiedPropertiesWithoutUndo();
            if(kind==0){
                Ref(root.GetComponent<Sentinel_Trap>(),"trapModel",emitter.gameObject);
                var old=new SerializedObject(root.GetComponent<Sentinel_Trap>()).FindProperty("scanSphereVisual").objectReferenceValue as GameObject;if(old)old.SetActive(false);
                var scan=visual.Find("ScanWave");if(!scan){scan=((GameObject)PrefabUtility.InstantiatePrefab(wave)).transform;scan.SetParent(visual,false);scan.name="ScanWave";}scan.gameObject.SetActive(false);
                Ref(root.GetComponent<Sentinel_Trap>(),"scanSphereVisual",scan.gameObject);
            }
            if(kind==1){var field=Field(root.transform,"SlowField");Ref(root.GetComponent<Sentinel_SlowTrap>(),"_field",field);field.gameObject.SetActive(false);}
            if(kind==2)Ref(root.GetComponent<TrapDevice>(),"energyEffectPrefab",wave);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void HUD()
    {
        var root=PrefabUtility.LoadPrefabContents(Hud);
        try{
            var ui=Ensure<SentinelFeedbackUI>(root);
            var owner=ScannerFeedbackAuthoring.Group(root.transform,"SentinelDeviceNotice",-200,250,36);((RectTransform)owner.transform).anchoredPosition=new Vector2(-430,-200);
            ScannerFeedbackAuthoring.Bar(owner.transform,"Backing",0,0,250,36,new Color(.07f,.055f,.09f,.84f));
            ScannerFeedbackAuthoring.Bar(owner.transform,"Signal",-123,0,3,36,new Color(1,.8f,.3f));
            var text=ScannerFeedbackAuthoring.Label(owner.transform,"Message","감지됨 · 스캔 준비",0,0,236,30,18,new Color(.96f,.9f,1));
            var status=ScannerFeedbackAuthoring.Group(root.transform,"SentinelConcussion",-240,180,38);
            ScannerFeedbackAuthoring.Bar(status.transform,"Backing",0,0,180,38,new Color(.09f,.055f,.11f,.82f));
            var label=ScannerFeedbackAuthoring.Label(status.transform,"Status","진탕  3.0초",0,3,174,30,18,new Color(1,.85f,.45f));
            var fill=ScannerFeedbackAuthoring.Bar(status.transform,"Remaining",0,-15,170,2,new Color(.76f,.45f,1));fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;
            Ref(ui,"ownerNotice",owner);Ref(ui,"statusNotice",status);Ref(ui,"ownerText",text);Ref(ui,"statusText",label);Ref(ui,"statusFill",fill);
            Ref(ui,"noticeAudio",ScannerFeedbackAuthoring.Sound(root.transform,"SentinelNoticeAudio","Confirmed",.32f,.8f));
            ui.Clear();PrefabUtility.SaveAsPrefabAsset(root,Hud);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Motion()
    {
        var path=Prefabs+"Sentinel.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var cameraRoot=root.transform.Find("CameraRoot");
            var bones=cameraRoot.GetComponentsInChildren<Transform>(true);
            Transform Bone(string suffix)=>bones.FirstOrDefault(t=>t.name=="mixamorig:"+suffix);
            var upper=Bone("LeftArm");var fore=Bone("LeftForeArm");var hand=Bone("LeftHand");
            Check(upper&&fore&&hand,"existing first person left-hand skeleton resolved");
            var camera=root.GetComponent<PlayerCameraController>().PlayerCamera.transform;
            var rig=Ensure<SentinelCastMotion>(root);Ref(rig,"upper",upper);Ref(rig,"forearm",fore);Ref(rig,"hand",hand);Ref(rig,"frame",camera);
            var held=Node(camera,"SentinelHeldDevice");held.localPosition=new Vector3(-.26f,-.24f,.43f);held.localScale=Vector3.one*.32f;
            held.localRotation=Quaternion.Euler(12,0,-12);
            Shape(held,"Shell",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.5f,.09f,.5f),purple);
            Shape(held,"Emitter",PrimitiveType.Sphere,new Vector3(0,.12f,0),new Vector3(.17f,.1f,.17f),glow);
            Ring(held,"ChargeCircuit",.2f,.105f,gold,64,.85f,.016f);
            Ref(rig,"heldDevice",held);held.gameObject.SetActive(false);
            var burst=root.transform.Find("SentinelOverchargeBurst");
            if(!burst){burst=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/EnergyWave.prefab"))).transform;burst.SetParent(root.transform,false);burst.name="SentinelOverchargeBurst";}
            burst.localPosition=Vector3.up;burst.gameObject.SetActive(false);Ref(rig,"overchargeBurst",burst);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Close the project before batch authoring.");
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        armor=Mat("GraphiteArmor",new Color(.13f,.14f,.16f));purple=Mat("VioletShell",new Color(.32f,.12f,.54f));
        gold=Mat("SignalGold",new Color(1,.72f,.2f),true);glow=Mat("IonPurple",new Color(.65f,.3f,1),true);
        var wave=Wave();Device("Sentinel_Trap",0,wave);Device("SlowTrap",1,wave);Device("TrapDevice",2,wave);
        var area=PrefabUtility.LoadPrefabContents(Prefabs+"X_Area.prefab");
        try{var f=Field(area.transform,"OverchargeField");Ref(area.GetComponent<X_Area>(),"field",f);foreach(var line in area.GetComponentsInChildren<LineRenderer>())if(!line.transform.IsChildOf(f.transform))line.enabled=false;PrefabUtility.SaveAsPrefabAsset(area,Prefabs+"X_Area.prefab");}
        finally{PrefabUtility.UnloadPrefabContents(area);}
        HUD();Motion();AssetDatabase.SaveAssets();Verify();Render();Debug.Log("SENTINEL_AUTHORING_PASS");
    }
    static void Check(bool valid,string message){if(!valid)throw new Exception(message);Debug.Log("SENTINEL_CHECK "+message);}
    public static void Verify()
    {
        var hud=AssetDatabase.LoadAssetAtPath<GameObject>(Hud);
        Check(hud.GetComponent<SentinelFeedbackUI>()&&hud.GetComponent<ElectricStatusUI>()&&hud.GetComponent<ScannerFeedbackUI>()&&hud.GetComponent<DischargeDirector>(),"existing status, scanner and discharge UI retained");
        Check(hud.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"HUD does not intercept gameplay clicks");
        foreach(var name in new[]{"Sentinel_Trap","SlowTrap","TrapDevice"}){
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+name+".prefab");Check(p.GetComponent<SentinelDeviceVfx>()&&p.GetComponentInChildren<Collider>(true),name+" saved presentation and original hit collider");
            Check(p.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.shader.name!="Hidden/InternalErrorShader"),name+" materials resolved");
        }
        Check(!File.ReadAllText("Assets/ProjectVector/Runtime/Characters/Sentinel/TrapDevice.cs").Contains("ApplyStunFromServer"),"E no longer uses stun");
        string guid=AssetDatabase.AssetPathToGUID(Hud);Check(File.ReadAllText("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity").Contains(guid),"authored HUD linked in actual game scene");
        Debug.Log("SENTINEL_VERIFY_PASS");
    }
    public static void Render()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("PreviewCamera").AddComponent<Camera>();camera.fieldOfView=25;camera.transform.position=new Vector3(1.5f,1.8f,-2.4f);camera.transform.LookAt(new Vector3(0,.16f,0));camera.backgroundColor=new Color(.22f,.21f,.24f);camera.clearFlags=CameraClearFlags.SolidColor;
        var light=new GameObject("KeyLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=new Color(.55f,.55f,.55f);
        string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"SentinelPreview");Directory.CreateDirectory(folder);
        foreach(var name in new[]{"Sentinel_Trap","SlowTrap","TrapDevice"}){
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+name+".prefab"));
            if(name=="TrapDevice")go.transform.rotation=Quaternion.Euler(-90,0,0);
            foreach(var c in go.GetComponentsInChildren<Canvas>(true))c.gameObject.SetActive(false);
            var rt=new RenderTexture(1000,750,24);var pixels=new Texture2D(1000,750,TextureFormat.RGB24,false);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1000,750),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);Object.DestroyImmediate(go);
        }
    }
}
#endif
