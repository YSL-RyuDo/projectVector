#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class ConstructionMapSurvey
{
    public static readonly string Output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Vector-Construction-20261026");
    public static Bounds BoundsOf(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>().Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
        Bounds b = rs.Length == 0 ? new Bounds(go.transform.position, Vector3.zero) : rs[0].bounds;
        foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }
    public static void Lighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.65f,.72f,.82f);
        RenderSettings.ambientEquatorColor = new Color(.45f,.49f,.55f);
        RenderSettings.ambientGroundColor = new Color(.3f,.32f,.36f);
        RenderSettings.fog = false;
        var light = new GameObject("Daylight").AddComponent<Light>();
        light.type=LightType.Directional;light.intensity=1.5f;light.color=new Color(1f,.95f,.86f);
        light.transform.rotation=Quaternion.Euler(48,-32,0);light.shadows=LightShadows.Soft;
    }
    public static void Capture(string name, Vector3 eye, Vector3 target, int width=1280, int height=720, float ortho=0,int cullingMask=~0)
    {
        var camera=new GameObject("Survey Camera").AddComponent<Camera>();
        camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
        camera.nearClipPlane=.05f;camera.farClipPlane=600;camera.fieldOfView=60;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.62f,.73f,.83f);
        camera.orthographic=ortho>0;camera.orthographicSize=ortho;
        camera.cullingMask=cullingMask;
        var rt=new RenderTexture(width,height,24){antiAliasing=4};camera.targetTexture=rt;
        camera.Render();RenderTexture.active=rt;
        var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();
        Directory.CreateDirectory(Output);File.WriteAllBytes(Path.Combine(Output,name+".png"),tex.EncodeToPNG());
        RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Survey is batch-only; never replace an unsaved user scene.");
        Directory.CreateDirectory(Output);var report=new StringBuilder();
        foreach(var name in new[]{"TestMap","Desert"})
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Lighting();
            var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/"+name+".prefab"));
            map.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);
            report.AppendLine(name+" all-render bounds "+BoundsOf(map));
            foreach(var p in info.attackSpawnPoints.Concat(info.defendSpawnPoints))report.AppendLine($"spawn {p.name}: {p.position:F3}, yaw {p.eulerAngles.y}");
            foreach(var z in map.GetComponentsInChildren<SpikePlantZone>(true))report.AppendLine($"site {z.name}: {z.GetComponent<BoxCollider>().bounds}");
            var refs=map.GetComponentInChildren<MiniMapWorldReferences>(true);var so=new SerializedObject(refs);
            report.AppendLine("minimapSize="+so.FindProperty("fullMapSize").floatValue+" center="+((Transform)so.FindProperty("fullMapCenter").objectReferenceValue).position);
            var rs=map.GetComponentsInChildren<Renderer>().Where(r=>r.bounds.size.x<150 && r.bounds.size.z<150 && !r.name.ToLowerInvariant().Contains("rock")).ToArray();
            Bounds b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);report.AppendLine("filtered bounds "+b);
            Capture(name+"-top",b.center+Vector3.up*130,b.center,1000,1000,Mathf.Max(b.extents.x,b.extents.z)+4);
        }
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ThirdParty/FPS_Bundle/FPS_Map5/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(s=>s))
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Lighting();
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var b=BoundsOf(go);report.AppendLine($"ASSET {path} | center={b.center:F3} size={b.size:F3} colliders={go.GetComponentsInChildren<Collider>().Length}");
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>{if(m==null)return m;var c=new Material(Shader.Find("Universal Render Pipeline/Lit"));var t=m.HasProperty("_BaseMap")?m.GetTexture("_BaseMap"):m.mainTexture;c.SetTexture("_BaseMap",t);c.SetFloat("_Smoothness",0);return c;}).ToArray();
            if(path.Contains("House")||path.Contains("Wall_0")||path.Contains("Crane")||path.Contains("Scaffolding")||path.Contains("Cement")||path.Contains("ConstructionMat")||path.Contains("Floor_01"))
                Capture("asset-"+go.name,b.center+new Vector3(1,.65f,-1).normalized*Mathf.Max(b.size.magnitude,3),b.center,600,450);
            if(path.Contains("House_Main"))foreach(var tr in go.GetComponentsInChildren<Transform>())report.AppendLine("CHILD "+tr.name);
        }
        File.WriteAllText(Path.Combine(Output,"survey.txt"),report.ToString());Debug.Log("CONSTRUCTION_SURVEY_PASS "+Output);
    }
    public static void Demo()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        var scene=EditorSceneManager.OpenScene("Assets/ThirdParty/FPS_Bundle/FPS_Map5/Scenes/LowPolyFPS6_Demo.unity");
        var b=new StringBuilder();
        foreach(var go in scene.GetRootGameObjects()){
            b.AppendLine(go.name+" "+BoundsOf(go));
            foreach(var c in go.GetComponentsInChildren<Camera>(true))c.enabled=false;
            foreach(var l in go.GetComponentsInChildren<Light>(true))l.enabled=false;
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>{if(!m)return m;var copy=new Material(Shader.Find("Universal Render Pipeline/Lit"));if(m.HasProperty("_BaseMap"))copy.SetTexture("_BaseMap",m.GetTexture("_BaseMap"));copy.SetFloat("_Smoothness",0);return copy;}).ToArray();
            foreach(var t in go.GetComponentsInChildren<Transform>())if(t.GetComponent<MeshRenderer>())b.AppendLine(t.name+" pos="+t.position+" size="+t.GetComponent<Renderer>().bounds.size);
        }
        Lighting();Capture("Bundle-Demo",new Vector3(80,75,-90),new Vector3(0,8,0),1500,1000);
        File.WriteAllText(Path.Combine(Output,"demo.txt"),b.ToString());
    }
}
#endif
