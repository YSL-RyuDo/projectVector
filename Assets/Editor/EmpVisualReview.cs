#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class EmpVisualReview
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("EMP_CHECK "+message);}
    static Camera CameraAt(Vector3 position,Vector3 look)
    {
        var c=new GameObject("ReviewCamera").AddComponent<Camera>();c.transform.position=position;c.transform.LookAt(look);c.fieldOfView=50;c.nearClipPlane=.02f;c.farClipPlane=300;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.055f,.075f,.095f);return c;
    }
    static void Light()
    {
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.white*.55f;
        var sun=new GameObject("ReviewKey").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(48,-35,0);
        var rim=new GameObject("ReviewRim").AddComponent<Light>();rim.type=LightType.Directional;rim.intensity=.7f;rim.color=new Color(.35f,.7f,1);rim.transform.rotation=Quaternion.Euler(-25,145,0);
    }
    static void Save(Camera camera,string name)
    {
        Directory.CreateDirectory(EmpPolishAuthoring.Output);var rt=new RenderTexture(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(EmpPolishAuthoring.Output+"/"+name+".png",tex.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
    }
    static GameObject Actor(string name,Vector3 position)
    {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Character/{name}/{name}.prefab"));root.transform.position=position;
        foreach(Transform child in root.transform)if(child.name!="Model" && child.name!="EMP" && child.name!="ElectricStatusFX")child.gameObject.SetActive(false);
        var r=root.GetComponent<Renderer>();if(r)r.enabled=false;
        foreach(var c in root.GetComponentsInChildren<Camera>(true))c.enabled=false;
        var animator=root.transform.Find("Model")?.GetComponentInChildren<Animator>();
        string clipName=name=="Sentinel"?"Sentinel_Lobby_Idle":name+"_Idle";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/Character Animation/{name}/Lobby/{clipName}.anim");if(animator&&clip)clip.SampleAnimation(animator.gameObject,.3f);
        return root;
    }
    public static void Run()
    {
        Check(Application.isBatchMode,"isolated batch");EmpPolishAuthoring.Run();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Light();
        var device=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EMP/WristEmitter.prefab"));
        Check(device.GetComponentsInChildren<Collider>().Length==0,"device cannot obstruct player collision or raycasts");
        Check(device.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial && r.sharedMaterial.shader.isSupported),"device uses supported materials");
        var c=CameraAt(new Vector3(.34f,.38f,.45f),Vector3.zero);c.fieldOfView=37;Save(c,"Emitter-Model");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Light();
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/TestMap.prefab"));map.transform.position=Vector3.zero;
        var info=map.GetComponent<MapInfo>();if(info.barriers)info.barriers.SetActive(false);
        Vector3 origin=info.GetSpawnPoint(0,0).position;origin.y+=.05f;
        var caster=Actor("Initiator",origin);var target=Actor("Sentinel",origin+new Vector3(3.7f,0,2));var second=Actor("Entry",origin+new Vector3(-2,0,4));
        var fx=caster.GetComponent<EmpCastVfx>();
        fx.Confirm(origin,Array.Empty<Vector3>());Check(fx.Playing && fx.ConfirmedTargets==0,"empty confirmed cast still emits wave");
        var rings=(LineRenderer[])typeof(EmpCastVfx).GetField("_waveRings",Flags).GetValue(fx);
        fx.Tick(.4f);Check(Mathf.Abs(Vector3.Distance(new Vector3(rings[0].GetPosition(0).x,origin.y,rings[0].GetPosition(0).z),origin)-7)<.01f,"wave stops at actual 7 metre radius");
        fx.ResetPresentation();Check(rings.All(r=>!r.enabled),"disable clears all cast rings");
        fx.Confirm(origin,new[]{target.transform.position+Vector3.up*1.1f,second.transform.position+Vector3.up*1.1f});fx.Tick(.13f);
        var arcs=(LineRenderer[])typeof(EmpCastVfx).GetField("_targetArcs",Flags).GetValue(fx);
        Check(arcs.Count(a=>a.enabled)==2,"two confirmed targets use two independent arcs");
        var originalEnd=arcs[0].GetPosition(63);target.transform.position+=Vector3.right;fx.Tick(.01f);
        Check(Vector3.Distance(originalEnd,arcs[0].GetPosition(63))<.001f,"visual does not track moving targets through walls");target.transform.position-=Vector3.right;
        foreach(var p in caster.GetComponentsInChildren<ParticleSystem>())if(p.isPlaying)p.Simulate(.14f,false,true);
        c=CameraAt(origin+new Vector3(6,3.8f,-6),origin+new Vector3(0,1,1));Save(c,"Cast-ThirdPerson");
        // Display the saved HUD using a review camera, not an invented UI mockup.
        var ui=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EMP/ElectricStatusHUD.prefab"));
        var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=c;canvas.planeDistance=.3f;
        ui.GetComponent<ElectricStatusUI>().Present(0,2.4f,3);Canvas.ForceUpdateCanvases();
        Check(ui.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"status HUD cannot intercept input");
        target.transform.Find("Model").gameObject.SetActive(false);
        c.transform.position=target.transform.position+Vector3.up*1.65f;c.transform.LookAt(origin+Vector3.up*1.25f);c.fieldOfView=70;Save(c,"Victim-Status");
        ui.GetComponent<ElectricStatusUI>().Hide();
        Check(!ui.transform.Find("LocalStatus").gameObject.activeSelf,"recovery hides status");
        for(int i=0;i<3;i++){fx.Confirm(origin,new[]{originalEnd});fx.Tick(1);Check(arcs.All(a=>!a.enabled) && rings.All(r=>!r.enabled),"cast cleanup "+i);}
        Debug.Log("EMP_VISUAL_PASS: saved device, actual TestMap preview, empty/confirmed casts, exact radius, independent arcs, expiry, no target tracking, HUD click-through");
    }
}
#endif
