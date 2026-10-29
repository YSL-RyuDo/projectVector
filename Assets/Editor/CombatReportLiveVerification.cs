#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CombatReportLiveVerification
{
    public static void ProbeCapture()
    {
        if(!Application.isBatchMode)throw new System.Exception("Batch only");
        EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var map=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/TestMap.prefab"));
        var spawn=map.GetComponent<MapInfo>().attackSpawnPoints[0];
        var light=new GameObject("EvidenceProbeLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-25,0);
        RenderSettings.ambientLight=Color.gray;RenderSettings.fog=false;
        var camera=new GameObject("EvidenceProbeCamera").AddComponent<Camera>();camera.enabled=false;
        camera.transform.SetPositionAndRotation(spawn.position+Vector3.up*1.5f,spawn.rotation);camera.fieldOfView=65;
        var hud=Object.FindFirstObjectByType<InGameUIManager>(FindObjectsInactive.Include);
        for(var t=hud.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
        hud.SetHpBarrier(74,25,100,50);hud.SetBulletText(17,75);
        string output=System.Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT")+"/Probe";System.IO.Directory.CreateDirectory(output);
        var runner=new GameObject("EvidenceProbe").AddComponent<RankedCaptureTest>();
        const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        typeof(RankedCaptureTest).GetField("frames",flags).SetValue(runner,output);
        using(var timeline=new System.IO.StreamWriter(output+"/timeline.csv"))
        {
            timeline.WriteLine("frame,utcMilliseconds");typeof(RankedCaptureTest).GetField("timeline",flags).SetValue(runner,timeline);
            typeof(RankedCaptureTest).GetMethod("CaptureCameraFrame",flags).Invoke(runner,new object[]{camera,new Texture2D(960,540,TextureFormat.RGB24,false)});
        }
        Debug.Log("EVIDENCE_PROBE_COMPLETE");
    }
    public static void PrepareAndBuild()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Batch only");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var report=Object.FindFirstObjectByType<ReportUI>(FindObjectsInactive.Include);
        var root=ShopReadabilityAuthoring.Get<GameObject>(report,"rootPanel");
        var group=root.GetComponent<CanvasGroup>();if(!group)group=root.AddComponent<CanvasGroup>();
        ShopReadabilityAuthoring.Ref(report,"presentation",group);
        var observer=Object.FindFirstObjectByType<SpectatorHUD>(FindObjectsInactive.Include);
        group=observer.root.GetComponent<CanvasGroup>();if(!group)group=observer.root.AddComponent<CanvasGroup>();
        ShopReadabilityAuthoring.Ref(observer,"presentation",group);
        // Inventory stays above the first-person hand/weapon silhouette. Keep the
        // authored slot objects, sprites and existing inventory bindings intact.
        var hud=Object.FindFirstObjectByType<InGameUIManager>(FindObjectsInactive.Include);
        var hudData=new SerializedObject(hud);var slots=hudData.FindProperty("weaponSlotBackgrounds");
        for(int i=0;i<slots.arraySize;i++)
        {
            var row=(UnityEngine.UI.Image)slots.GetArrayElementAtIndex(i).objectReferenceValue;
            var rect=row.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(1,0);
            rect.anchoredPosition=new Vector2(-147,326+i*58);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        CombatReportVerification.Run();
        RankedCaptureTestEditor.Build();
    }
}
#endif
