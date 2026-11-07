#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ControllerSmokeVerification
{
    public const string Output="C:/Users/GCT/Documents/ControllerSmokeDirectional-20261023";
    static void Check(bool value,string label){if(!value)throw new Exception("SMOKE_POLISH_FAIL "+label);Debug.Log("SMOKE_POLISH_CHECK "+label);}
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static GameObject Load(string path)=>(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    static void Light(){RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.white*.7f;var l=new GameObject("ReviewSun").AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.4f;l.transform.rotation=Quaternion.Euler(48,-30,0);}
    static Camera CameraAt(Vector3 p,Vector3 look){var c=new GameObject("ReviewCamera").AddComponent<Camera>();c.transform.position=p;c.transform.LookAt(look);c.fieldOfView=65;c.nearClipPlane=.02f;c.farClipPlane=500;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.28f,.38f,.48f);c.GetUniversalAdditionalCameraData().requiresDepthTexture=true;return c;}
    static void Save(Camera camera,string name)
    {
        Directory.CreateDirectory(Output);var rt=new RenderTexture(1280,720,24);var t=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=camera.targetTexture;
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1280,720),0,0);t.Apply();File.WriteAllBytes(Output+"/"+name+".png",t.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=old;Object.DestroyImmediate(rt);Object.DestroyImmediate(t);
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        ControllerSmokeAuthoring.Run();
        VerifySaved();
    }
    public static void VerifySaved()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        var shader=Shader.Find("Vector/ControllerSmoke");Check(shader&&shader.isSupported,"volume shader supported");
        Check(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"shader compiles");
        var brushMaterial=AssetDatabase.LoadAssetAtPath<Material>(ControllerSmokeAuthoring.Dir+"/SmokeVolume.mat");
        Check(brushMaterial.GetTexture("_BrushMap"),"painted smoke mask is saved on production material");
        var brushImporter=(TextureImporter)AssetImporter.GetAtPath(ControllerSmokeAuthoring.Dir+"/SmokeBrushMask.png");
        Check(!brushImporter.sRGBTexture&&brushImporter.mipmapEnabled&&brushImporter.wrapMode==TextureWrapMode.Mirror,"brush mask is linear, mipmapped and mirrored");
        BoundaryPixels();
        foreach(string mapName in new[]{"TestMap","Desert"}){
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Light();
            var map=Load("Assets/ProjectVector/Shared/Maps/Prefabs/"+mapName+".prefab");var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);
            var spawn=info.attackSpawnPoints[0];Vector3 position=spawn.position+spawn.forward*6;
            Physics.SyncTransforms();if(Physics.Raycast(position+Vector3.up*12,Vector3.down,out var ground,40,~0,QueryTriggerInteraction.Ignore))position=ground.point;
            var smoke=Load(ControllerSmokeAuthoring.Dir+"/ControllerSmoke.prefab");smoke.transform.position=position;smoke.transform.localScale=Vector3.one*4.5f;smoke.GetComponent<ControllerSmokeVfx>().Begin(18);smoke.GetComponent<ControllerSmokeVfx>().Advance(1);
            Check(smoke.GetComponentsInChildren<Collider>().Length==0,mapName+" smoke never blocks shots/movement");
            Check(Mathf.Approximately(smoke.GetComponent<SmokeOutlineOccluder>().WorldRadius,4.5f),mapName+" visual matches server 4.5m radius");
            Vector3 eye=position+new Vector3(9,4,-11);
            for(int angle=0;angle<360;angle+=30){var candidate=position+Quaternion.Euler(0,angle,0)*new Vector3(0,4,-13);if(!Physics.Linecast(candidate,position+Vector3.up*1.5f,~0,QueryTriggerInteraction.Ignore)){eye=candidate;break;}}
            var forward=Vector3.ProjectOnPlane(position-eye,Vector3.up).normalized;
            var person=PreviewPerson(position+forward*1.1f,Quaternion.LookRotation(-forward));
            var c=CameraAt(eye,position+Vector3.up*1.5f);Save(c,mapName+"-smoke-outside");
            var bodyBounds=person.GetComponentsInChildren<SkinnedMeshRenderer>().First().bounds;
            foreach(var skin in person.GetComponentsInChildren<SkinnedMeshRenderer>())bodyBounds.Encapsulate(skin.bounds);
            c.fieldOfView=80;
            c.transform.position=position-forward*2f+Vector3.up*Mathf.Clamp(bodyBounds.size.y*.78f,1.6f,2.8f);
            c.transform.LookAt(bodyBounds.center);Save(c,mapName+"-smoke-inside");
            smoke.SetActive(false);Save(c,mapName+"-inside-without-smoke-reference");smoke.SetActive(true);
            Object.DestroyImmediate(person);
        }
        Hand();UI();Palette();
        Debug.Log("CONTROLLER_SMOKE_POLISH_PASS");
        SmokeOutlineVerification.Run(); // Finish with real Play/render callback regressions.
    }
    static GameObject PreviewPerson(Vector3 position,Quaternion rotation)
    {
        var actor=Load(ControllerSmokeAuthoring.Actor);
        var model=actor.transform.Find("Model");Check(model,"third-person model for inside visibility capture");
        var preview=Object.Instantiate(model.gameObject);Object.DestroyImmediate(actor);
        preview.transform.SetPositionAndRotation(position,rotation);preview.SetActive(true);
        foreach(var r in preview.GetComponentsInChildren<Renderer>(true)){r.enabled=true;r.forceRenderingOff=false;}
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ProjectVector/Shared/Characters/Models/Controller/Animation/InGame/Pistol/Controller_Pistol_Idle.anim");
        if(clip)clip.SampleAnimation(preview,0);
        return preview;
    }
    static Color CenterPixel(Camera c)
    {
        var rt=new RenderTexture(256,256,24);var tex=new Texture2D(256,256,TextureFormat.RGB24,false);
        var previous=c.targetTexture;var active=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
        tex.ReadPixels(new Rect(0,0,256,256),0,0);tex.Apply();var color=tex.GetPixel(128,128);
        RenderTexture.active=active;c.targetTexture=previous;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);return color;
    }
    static void BoundaryPixels()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var smoke=Load(ControllerSmokeAuthoring.Dir+"/ControllerSmoke.prefab");smoke.transform.localScale=Vector3.one*4.5f;
        smoke.GetComponent<ControllerSmokeVfx>().Begin(18);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.localScale=new Vector3(.7f,.7f,.2f);
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",Color.green);target.GetComponent<Renderer>().sharedMaterial=material;
        var c=CameraAt(new Vector3(0,1.6f,-1),new Vector3(0,1.6f,1));c.backgroundColor=Color.black;
        void Test(Vector3 eye,Vector3 point,bool visible,string label){c.transform.position=eye;c.transform.rotation=Quaternion.identity;target.transform.position=point;var color=CenterPixel(c);bool green=color.g>color.r+.25f&&color.g>color.b+.25f;Check(green==visible,label+" pixel="+color);if(!visible)Check(color.maxColorComponent>.2f,label+" boundary is not black");}
        Test(new Vector3(0,1.6f,-1),new Vector3(0,1.6f,1),true,"inside sees nearby player depth");
        Test(new Vector3(0,1.6f,-1),new Vector3(0,1.6f,7),false,"inside cannot see outside across boundary");
        Test(new Vector3(0,1.6f,-7),new Vector3(0,1.6f,0),false,"outside cannot see player inside");
        Test(new Vector3(0,1.6f,-7),new Vector3(0,1.6f,7),false,"outside cannot see through entire smoke");
        Test(new Vector3(0,1.6f,-7),new Vector3(0,1.6f,-6),true,"outside foreground geometry not covered");
        foreach(float z in new[]{-4.4f,-4.2f,-4f})Test(new Vector3(0,1.6f,z),new Vector3(0,1.6f,7),false,"crossing boundary conceals far target "+z);
        float previousAge=0;
        foreach(float age in new[]{0f,2f,7f,16f})
        {
            smoke.GetComponent<ControllerSmokeVfx>().Advance(age-previousAge);previousAge=age;
            Test(new Vector3(0,1.6f,-7),new Vector3(0,1.6f,7),false,"animated core never exposes enemy age="+age);
            Test(new Vector3(4.2f,0,-7),new Vector3(4.2f,0,7),false,"grazing opaque boundary conceals enemy age="+age);
            Test(new Vector3(0,1.6f,-1),new Vector3(0,1.6f,1),true,"animated interior keeps nearby geometry visible age="+age);
        }
        Object.DestroyImmediate(target);Object.DestroyImmediate(material);Object.DestroyImmediate(c.gameObject);Object.DestroyImmediate(smoke);
        Debug.Log("SMOKE_BOUNDARY_PIXELS_PASS");
    }
    static void Hand()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Light();
        var actor=Load(ControllerSmokeAuthoring.Actor);var model=actor.transform.Find("CameraRoot/Controller Model_Riging (1)");
        var real=actor.GetComponent<PlayerCameraController>().PlayerCamera;var c=CameraAt(real.transform.position,real.transform.position+real.transform.forward);c.fieldOfView=90;c.cullingMask=real.cullingMask;
        foreach(var camera in actor.GetComponentsInChildren<Camera>(true))camera.enabled=false;
        foreach(Transform child in actor.transform)if(child.name!="CameraRoot")child.gameObject.SetActive(false);
        var rootRenderer=actor.GetComponent<Renderer>();if(rootRenderer)rootRenderer.enabled=false;
        actor.GetComponent<PlayerItem>()?.SetWeaponVisibility(false);
        foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
            r.enabled=r.name=="Arm1"||r.name=="Arm2";if(r.enabled){r.gameObject.SetActive(true);r.forceRenderingOff=false;}
        }
        var motion=actor.GetComponent<ControllerSmokeMotion>();var bones=Field<Transform[]>(motion,"_bones");var before=bones.Select(t=>t.localPosition).ToArray();var rot=bones.Select(t=>t.localRotation).ToArray();
        motion.Begin();motion.Advance(.3f);Save(c,"Right-hand-ready");motion.Release();motion.Advance(.08f);Save(c,"Right-hand-command");
        var hand=bones.First(t=>t.name=="mixamorig:RightHand");var point=c.WorldToViewportPoint(hand.position);Check(point.x>.5f&&point.x<.95f&&point.y>0&&point.y<.55f,"right hand enters lower right "+point);
        for(int i=1;i<bones.Length;i++)Check(Vector3.Distance(before[i],bones[i].localPosition)<.0001f,"no bone stretching "+bones[i].name);
        motion.Advance(.5f);Check(!motion.Active,"release completes");
        for(int i=0;i<bones.Length;i++)Check(Vector3.Distance(before[i],bones[i].localPosition)<.0001f&&Quaternion.Angle(rot[i],bones[i].localRotation)<.1f,"weapon pose restored "+bones[i].name);
    }
    static void UI()
    {
        var scene=EditorSceneManager.OpenScene(ControllerSmokeAuthoring.ScenePath,OpenSceneMode.Single);
        var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SmokeTargetingView>(true)).Single();
        // Isolate only the saved overlay in a review scene; never save these review settings.
        var clone=Object.Instantiate(view.gameObject);
        clone.transform.SetParent(null);var overlay=clone.GetComponent<SmokeTargetingView>();
        var roots=scene.GetRootGameObjects();foreach(var root in roots)if(root!=clone)root.SetActive(false);
        var map=Load("Assets/ProjectVector/Shared/Maps/Prefabs/Desert.prefab");var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);Light();
        var bounds=new Bounds(info.attackSpawnPoints[0].position,Vector3.one);foreach(var spawn in info.attackSpawnPoints.Concat(info.defendSpawnPoints))bounds.Encapsulate(spawn.position);
        var mapCamera=CameraAt(bounds.center+Vector3.up*100,bounds.center);mapCamera.orthographic=true;mapCamera.orthographicSize=50;mapCamera.aspect=1;var texture=new RenderTexture(1024,1024,24);mapCamera.targetTexture=texture;mapCamera.Render();overlay.Map.GetComponent<RawImage>().texture=texture;
        var uiCamera=CameraAt(new Vector3(0,0,-200),Vector3.zero);uiCamera.cullingMask=1<<5;uiCamera.orthographic=true;uiCamera.orthographicSize=540;
        var canvas=AddCanvas(clone);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=uiCamera;canvas.planeDistance=1;
        foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
        clone.SetActive(true);var group=clone.GetComponent<CanvasGroup>();if(group)group.alpha=1;
        var animator=clone.GetComponent<UIAnimator>();if(animator)animator.enabled=false;
        int count=clone.GetComponentsInChildren<Transform>(true).Length;overlay.Select(new Vector2(60,-45),Vector2.one*64.8f,24);Canvas.ForceUpdateCanvases();Save(uiCamera,"Smoke-targeting");
        overlay.Invalid();overlay.ResetSelection();Check(count==clone.GetComponentsInChildren<Transform>(true).Length,"selection reuses saved hierarchy");
        Object.DestroyImmediate(texture);
    }
    static Canvas AddCanvas(GameObject go){var canvas=go.AddComponent<Canvas>();var scaler=go.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);return canvas;}
    static void Palette()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Light();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.15f,0);floor.transform.localScale=new Vector3(40,.3f,30);Physics.SyncTransforms();
        var swamp=Load("Assets/ProjectVector/Shared/Characters/Prefabs/Controller/TrapArea.prefab");swamp.transform.position=new Vector3(-5,.03f,0);
        var visual=swamp.GetComponentInChildren<SwampGroundMeshVisual>();Check(visual,"swamp ground visual retained");visual.Init(3,visual.GetComponent<Renderer>().sharedMaterial,~0);
        var trap=Load("Assets/ProjectVector/Shared/Characters/Prefabs/Controller/Trap.prefab");trap.transform.position=new Vector3(-5,.5f,0);
        var decoy=Load("Assets/ProjectVector/Shared/Characters/Prefabs/Controller/DecoyTrap.prefab");decoy.transform.position=new Vector3(0,.5f,0);
        var ultimate=Load("Assets/ProjectVector/Shared/Characters/Prefabs/Controller/DreamForestArea.prefab");ultimate.transform.position=new Vector3(6,0,0);ultimate.transform.localScale=Vector3.one*4;
        Check(ultimate.transform.Find("Sphere").GetComponent<Renderer>().sharedMaterial.shader==AssetDatabase.LoadAssetAtPath<Shader>("Assets/ProjectVector/Shared/Characters/Prefabs/Controller/DreamForestShader.shadergraph"),"ultimate uses controller palette graph, not legacy KSW duplicate");
        Save(CameraAt(new Vector3(1,10,-17),new Vector3(1,1,0)),"Controller-skill-palette");
        foreach(var r in new[]{swamp,trap,decoy,ultimate}.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))foreach(var m in r.sharedMaterials)Check(m&&m.shader.isSupported,"controller material supported "+r.name);
    }
}
#endif
