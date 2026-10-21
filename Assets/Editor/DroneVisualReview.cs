#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using System.Reflection;
public static class DroneVisualReview
{
    const string ActorPath="Assets/Prefab/Character/Initiator/Initiator.prefab";
    const string DronePath="Assets/Prefab/Character/Initiator/Drone.prefab";
    const string Materials="Assets/Materials/Drone";
    public static void PrepareCaptureBuild()
    {
        AuthorPose();
        RankedCaptureTestEditor.Build();
    }
    public static void AuthorShockReceivers()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(DronePath).transform.Find("ImpactSpark").GetComponent<ParticleSystem>();
        foreach(string character in new[]{"Initiator","Controller","Entry","Sentinel"}) {
            string path=$"Assets/Prefab/Character/{character}/{character}.prefab";
            var actor=PrefabUtility.LoadPrefabContents(path);
            try {
                var shock=actor.GetComponent<ElectricShock>();if(!shock)shock=actor.AddComponent<ElectricShock>();
                var node=actor.transform.Find("ElectricShockSparks");
                if(!node){node=new GameObject("ElectricShockSparks").transform;node.SetParent(actor.transform,false);}
                node.localPosition=new Vector3(0,1,0);
                var particles=node.GetComponent<ParticleSystem>();if(!particles)particles=node.gameObject.AddComponent<ParticleSystem>();
                particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                EditorUtility.CopySerialized(source,particles);
                var main=particles.main;main.playOnAwake=false;main.startSpeed=new ParticleSystem.MinMaxCurve(.4f,1.2f);main.startSize=new ParticleSystem.MinMaxCurve(.015f,.04f);
                var shape=particles.shape;shape.radius=.25f;
                particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=source.GetComponent<ParticleSystemRenderer>().sharedMaterial;
                var fields=new SerializedObject(shock);
                fields.FindProperty("_shockEffect").objectReferenceValue=particles;
                var skin=actor.transform.Find("Model")?.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault();
                if(skin){fields.FindProperty("_renderer").objectReferenceValue=skin;fields.FindProperty("_normalMaterial").objectReferenceValue=skin.sharedMaterial;}
                fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(actor,path);
                Debug.Log("DRONE_SHOCK_RECEIVER_PASS "+character);
            }finally{PrefabUtility.UnloadPrefabContents(actor);}
        }
        AssetDatabase.SaveAssets();
    }
    static Material Compatible(Material source)
    {
        string name=source?source.name:"DroneShell";
        string path=Materials+"/"+name+".mat";
        var result=AssetDatabase.LoadAssetAtPath<Material>(path);
        bool create=!result;
        if(create)result=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        result.SetColor("_BaseColor",source && source.HasProperty("_Color")?source.GetColor("_Color"):new Color(.12f,.17f,.2f));
        if(source && source.HasProperty("_MainTex"))result.SetTexture("_BaseMap",source.GetTexture("_MainTex"));
        result.SetFloat("_Metallic",.35f);result.SetFloat("_Smoothness",.45f);
        if(name=="Red")result.SetColor("_BaseColor",new Color(.075f,.13f,.17f));
        if(name=="White")result.SetColor("_BaseColor",new Color(.35f,.8f,.88f));
        if(create)AssetDatabase.CreateAsset(result,path);else EditorUtility.SetDirty(result);
        return result;
    }
    static void RepairMaterials(Transform visual)
    {
        foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>!m || m.shader.name=="Standard"?Compatible(m):m).ToArray();
    }
    public static void FixMaterials()
    {
        Directory.CreateDirectory(Materials);AssetDatabase.Refresh();
        foreach(string path in new[]{ActorPath,DronePath}) {
            var root=PrefabUtility.LoadPrefabContents(path);
            try {
                var visual=root.GetComponentsInChildren<Transform>(true).First(t=>t.name==(path==ActorPath?"DroneLaunchDevice":"drone"));
                RepairMaterials(visual);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("DRONE_MATERIAL_REPAIR_PASS");
        PoseReview();
    }
    public static void PoseReview()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ActorPath));
        actor.transform.Find("Model").gameObject.SetActive(false);
        var rootRenderer=actor.GetComponent<Renderer>();if(rootRenderer)rootRenderer.enabled=false;
        var camera=actor.GetComponentInChildren<Camera>(true);camera.enabled=true;camera.cullingMask=~0;camera.fieldOfView=90;camera.nearClipPlane=.03f;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.28f,.34f,.4f);
        var model=actor.transform.Find("CameraRoot/Controller Model_Riging (1)");
        foreach(var r in model.GetComponentsInChildren<Renderer>(true))if(r.name=="HeadUV" || r.name=="THair6UV")r.enabled=false;
        foreach(var animator in model.GetComponentsInChildren<Animator>(true))animator.enabled=false;
        var rig=actor.GetComponent<DroneLaunchRig>();
        typeof(DroneLaunchRig).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rig,null);
        rig.SetPrepared(true);
        for(int i=0;i<100;i++) typeof(DroneLaunchRig).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rig,new object[]{1f/120});
        var device=model.GetComponentsInChildren<Transform>(true).First(t=>t.name=="DroneLaunchDevice");
        var fore=device.parent;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.white*.7f;
        var light=new GameObject("ReviewSun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-35,0);
        typeof(DroneLaunchRig).GetMethod("ApplyPose",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rig,new object[]{1f});
        Save(camera,"Drone-Prepare-Fixed.png");
        Debug.Log("DRONE_POSE_REVIEW_PASS");
    }
    public static void AuthorPose()
    {
        Compatible(AssetDatabase.LoadAssetAtPath<Material>("Assets/KSW/Drone/Art/material/Red.mat"));
        Compatible(AssetDatabase.LoadAssetAtPath<Material>("Assets/KSW/Drone/Art/material/White.mat"));
        var actor=PrefabUtility.LoadPrefabContents(ActorPath);
        try {
            var model=actor.transform.Find("CameraRoot/Controller Model_Riging (1)");
            var cameraRoot=model.parent;
            var bones=model.GetComponentsInChildren<Transform>(true);
            var upper=bones.First(t=>t.name=="mixamorig:LeftArm");
            var fore=bones.First(t=>t.name=="mixamorig:LeftForeArm");
            var hand=bones.First(t=>t.name=="mixamorig:LeftHand");
            var right=bones.First(t=>t.name=="mixamorig:RightArm");
            var rightFore=bones.First(t=>t.name=="mixamorig:RightForeArm");
            var posed=new[]{upper,fore,right};
            var directions=new[]{new Vector3(.45f,.4f,.8f),new Vector3(.35f,.55f,.8f),new Vector3(0,-1,-.5f)};
            var ends=new[]{fore,hand,rightFore};
            var originals=posed.Select(t=>t.localRotation).ToArray();
            var targetRoot=cameraRoot.Find("DronePreparationPose");
            if(!targetRoot){targetRoot=new GameObject("DronePreparationPose").transform;targetRoot.SetParent(cameraRoot,false);}
            var anchor=targetRoot.Find("LeftShoulderAnchor");
            if(!anchor){anchor=new GameObject("LeftShoulderAnchor").transform;anchor.SetParent(targetRoot,false);}
            anchor.localPosition=new Vector3(-.45f,-.52f,.45f);
            var targets=new Transform[3];
            for(int i=0;i<3;i++) {
                var target=targetRoot.Find(posed[i].name);
                if(!target){target=new GameObject(posed[i].name).transform;target.SetParent(targetRoot,false);}
                posed[i].rotation=Quaternion.FromToRotation(ends[i].position-posed[i].position,cameraRoot.TransformDirection(directions[i]))*posed[i].rotation;
                target.rotation=posed[i].rotation;targets[i]=target;
            }
            var device=fore.Find("DroneLaunchDevice");
            device.rotation=cameraRoot.rotation*Quaternion.Euler(20,0,0);
            device.position=Vector3.Lerp(fore.position,hand.position,.5f)+cameraRoot.up*.09f;
            var bounds=device.GetComponentsInChildren<Renderer>(true)[0].bounds;
            foreach(var renderer in device.GetComponentsInChildren<Renderer>(true))bounds.Encapsulate(renderer.bounds);
            device.position+=Vector3.Lerp(fore.position,hand.position,.5f)+cameraRoot.up*.09f-bounds.center;
            for(int i=0;i<3;i++)posed[i].localRotation=originals[i];
            var rig=actor.GetComponent<DroneLaunchRig>();var serialized=new SerializedObject(rig);
            serialized.FindProperty("_holsterDuration").floatValue=.24f;
            serialized.FindProperty("_prepareDuration").floatValue=.35f;
            serialized.FindProperty("_launchDuration").floatValue=.28f;
            serialized.FindProperty("_returnDuration").floatValue=.24f;
            serialized.FindProperty("_drawDuration").floatValue=.26f;
            serialized.FindProperty("_viewModel").objectReferenceValue=model;
            serialized.FindProperty("_shoulderAnchor").objectReferenceValue=anchor;
            var fingers=hand.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig:")).ToArray();
            var handBones=serialized.FindProperty("_handBones");var handPose=serialized.FindProperty("_openHandPose");
            handBones.arraySize=handPose.arraySize=fingers.Length;
            for(int i=0;i<fingers.Length;i++) {
                handBones.GetArrayElementAtIndex(i).objectReferenceValue=fingers[i];
                handPose.GetArrayElementAtIndex(i).quaternionValue=fingers[i].localRotation;
            }
            serialized.FindProperty("_stowedShoulder").objectReferenceValue=bones.First(t=>t.name=="mixamorig:RightShoulder");
            serialized.FindProperty("_preparedViewOffset").vector3Value=new Vector3(-.38f,-.12f,.22f);
            foreach(var name in new[]{"_posedBones","_poseTargets"}) {
                var p=serialized.FindProperty(name);p.arraySize=3;
                for(int i=0;i<3;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=name=="_posedBones"?posed[i]:targets[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(actor,ActorPath);
        }finally{PrefabUtility.UnloadPrefabContents(actor);}
        AssetDatabase.SaveAssets();
        PoseReview();
    }
    static void Save(Camera camera,string name)
    {
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        var output="C:/Users/GCT/Downloads/Vector_Drone_20261021";Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
    }
    public static void Inspect()
    {
        var actor=PrefabUtility.LoadPrefabContents("Assets/Prefab/Character/Initiator/Initiator.prefab");
        try {
            foreach(var t in actor.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Left") || t.name.Contains("Camera")))
                Debug.Log($"RIG {AnimationUtility.CalculateTransformPath(t,actor.transform)} local={t.localPosition} world={t.position} scale={t.lossyScale} rotation={t.eulerAngles}");
            foreach(var r in actor.GetComponentsInChildren<Renderer>(true))
                Debug.Log($"RENDER {AnimationUtility.CalculateTransformPath(r.transform,actor.transform)} name={r.name} parent={r.transform.parent?.name} sameRoot={r.gameObject==actor} enabled={r.enabled} layer={r.gameObject.layer} source={PrefabUtility.GetCorrespondingObjectFromSource(r)} active={r.gameObject.activeInHierarchy} bounds={r.bounds} mats={string.Join(";",r.sharedMaterials.Select(m=>m?m.name+":"+m.shader.name:"NULL"))}");
        } finally {PrefabUtility.UnloadPrefabContents(actor);}
    }
}
#endif
