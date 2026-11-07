#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ConstructionMapIntegration
{
    const string PresentationPath="Assets/ProjectVector/Shared/Maps/Prefabs/ConstructionPresentation.prefab";
    static T Ref<T>(Object o,string name) where T:Object=>new SerializedObject(o).FindProperty(name).objectReferenceValue as T;
    static void Set(Object o,string name,Object value){var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Append(Object o,string name,Object value)
    {
        var s=new SerializedObject(o);var p=s.FindProperty(name);for(int i=0;i<p.arraySize;i++)if(p.GetArrayElementAtIndex(i).objectReferenceValue==value)return;
        p.InsertArrayElementAtIndex(p.arraySize);p.GetArrayElementAtIndex(p.arraySize-1).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();
    }
    static T[] All<T>() where T:Component=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
    static void BuildPresentationPrefab()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ConstructionMapAuthoring.MapPath));
        PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        go.GetComponent<MapInfo>().SetBarriersActive(false);
        foreach(var c in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))if(!(b is TMP_Text))Object.DestroyImmediate(b);
        foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        PrefabUtility.SaveAsPrefabAsset(go,PresentationPath);Object.DestroyImmediate(go);
    }
    static GameObject Preview(Transform parent,Vector3 origin)
    {
        var old=parent.Find("Construction");if(old)Object.DestroyImmediate(old.gameObject);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PresentationPath),parent);
        go.name="Construction";
        var p=AssetDatabase.LoadAssetAtPath<GameObject>(ConstructionMapAuthoring.MapPath).GetComponent<MapInfo>().attackSpawnPoints[0];
        go.transform.rotation=Quaternion.Inverse(p.rotation);go.transform.position=origin-go.transform.rotation*(p.position+p.forward*4);
        return go;
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only; preserve user's open scene");
        BuildPresentationPrefab();
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var rules=All<RoomRulesPanelUI>().Single();var a=Ref<Button>(rules,"testMapButton");var b=Ref<Button>(rules,"desertButton");
        var c=Ref<Button>(rules,"constructionButton");
        if(!c)
        {
            c=Object.Instantiate(b,b.transform.parent);c.name="ConstructionMapButton";c.onClick=new Button.ButtonClickedEvent();
            var ar=(RectTransform)a.transform;var br=(RectTransform)b.transform;var cr=(RectTransform)c.transform;
            var gap=br.anchoredPosition-ar.anchoredPosition;
            // Reuse the existing map selection region; no runtime-generated UI or panel replacement.
            if(Mathf.Abs(gap.x)>=Mathf.Abs(gap.y)){
                float left=ar.anchoredPosition.x-ar.sizeDelta.x/2;float right=br.anchoredPosition.x+br.sizeDelta.x/2;float w=(right-left-24)/3;
                for(int i=0;i<3;i++){var r=new[]{ar,br,cr}[i];r.sizeDelta=new Vector2(w,r.sizeDelta.y);r.anchoredPosition=new Vector2(left+w/2+i*(w+12),ar.anchoredPosition.y);}
            }else{
                float top=ar.anchoredPosition.y+ar.sizeDelta.y/2;float bottom=br.anchoredPosition.y-br.sizeDelta.y/2;float h=(top-bottom-20)/3;
                for(int i=0;i<3;i++){var r=new[]{ar,br,cr}[i];r.sizeDelta=new Vector2(r.sizeDelta.x,h);r.anchoredPosition=new Vector2(ar.anchoredPosition.x,top-h/2-i*(h+10));}
            }
            foreach(var t in c.GetComponentsInChildren<TMP_Text>(true)){t.text="공사장";t.enableAutoSizing=true;t.fontSizeMin=15;}
            Set(rules,"constructionButton",c);
        }
        rules.ApplySelectedMap("TestMap");EditorSceneManager.SaveScene(scene);
        scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var match=All<MatchManager>().Single();Append(match,"maps",AssetDatabase.LoadAssetAtPath<GameObject>(ConstructionMapAuthoring.MapPath));
        var pick=All<PickManager>().Single();var backdrop=All<PickMapBackdrop>().Single();
        var preview=Preview(backdrop.stageRoot.transform,Ref<Transform>(pick,"previewSpawnPoint").position);
        Array.Resize(ref backdrop.maps,3);backdrop.maps[2]=preview;
        var intro=All<MapIntroPresentation>().Single();Array.Resize(ref intro.startPoses,3);Array.Resize(ref intro.endPoses,3);
        for(int i=0;i<2;i++){
            var pose=new GameObject(i==0?"ConstructionIntroStart":"ConstructionIntroEnd").transform;pose.SetParent(preview.transform,false);
            pose.localPosition=i==0?new Vector3(50,35,-53):new Vector3(35,24,-35);
            pose.rotation=Quaternion.LookRotation(preview.transform.TransformPoint(new Vector3(0,1,2))-pose.position);
            if(i==0)intro.startPoses[2]=pose;else intro.endPoses[2]=pose;
        }
        backdrop.ApplyMap("TestMap");EditorUtility.SetDirty(backdrop);EditorUtility.SetDirty(intro);EditorSceneManager.SaveScene(scene);
        foreach(string path in new[]{"Assets/ProjectVector/Modules/Results/Scenes/Result.unity","Assets/ProjectVector/Modules/Results/Scenes/RankResult.unity"}){
            scene=EditorSceneManager.OpenScene(path);var stage=All<ResultCharacterStage>().FirstOrDefault();if(!stage)continue;
            var parent=stage.maps[0].transform.parent;var map=Preview(parent,stage.transform.position);
            Array.Resize(ref stage.maps,3);stage.maps[2]=map;stage.SelectMap("TestMap");EditorUtility.SetDirty(stage);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();Debug.Log("CONSTRUCTION_INTEGRATION_PASS private button, gameplay map, pick flyover, result stage");
    }
    public static void BuildAndVerify()
    {
        ConstructionMapAuthoring.Run();Run();VerificationAndExport();
    }
    public static void RebuildLayoutAndVerify()
    {
        // Existing gameplay/pick/result references keep their prefab GUIDs.
        // Layout changes must not reset lobby choices or rewrite unrelated scenes.
        var previousPipeline=QualitySettings.renderPipeline;int previousAA=QualitySettings.antiAliasing;
        try{ConstructionMapAuthoring.Run();BuildPresentationPrefab();VerificationAndExport();}
        finally{QualitySettings.renderPipeline=previousPipeline;QualitySettings.antiAliasing=previousAA;AssetDatabase.SaveAssets();}
    }
    public static void VerificationAndExport()
    {
        ConstructionMapVerification.Run();ConstructionMapEngagementAudit.Run();PlantZoneExport.Run();MovementSpawnExport.Run();ScannerOcclusionExport.Run();
    }
    static void ResizeAPlantZone(GameObject map)
    {
        var site=map.transform.Find("SpikePlantZoneA");
        if(!site)throw new Exception("Missing saved A plant zone in "+map.name);
        var oldCenter=site.localPosition;
        var center=ConstructionMapLayout.APlantCenter;
        var size=ConstructionMapLayout.APlantSize;
        var dressing=map.transform.Find("02_SetDressing");
        var edges=dressing.Cast<Transform>().Where(t=>t.name=="A_SiteEdge").ToArray();
        if(edges.Length!=4)throw new Exception("Expected four authored A boundary strips");
        // Spawn wayfinding also has a Mark_A object; only move the site-floor label.
        var label=dressing.Cast<Transform>().Single(t=>t.name=="Mark_A"&&Mathf.Abs(t.localPosition.x-oldCenter.x)<.01f&&Mathf.Abs(t.localPosition.z-oldCenter.z)<.01f);
        foreach(var edge in edges){
            var p=edge.localPosition;var scale=edge.localScale;
            if(scale.z>scale.x){p.x=center.x+Mathf.Sign(p.x-oldCenter.x)*size.x/2;p.z=center.z;scale.z=size.y;}
            else{p.x=center.x;p.z=center.z+Mathf.Sign(p.z-oldCenter.z)*size.y/2;scale.x=size.x;}
            edge.localPosition=p;edge.localScale=scale;
        }
        label.localPosition=new Vector3(center.x,label.localPosition.y,center.z);
        site.localPosition=new Vector3(center.x,oldCenter.y,center.z);
        var box=site.GetComponent<BoxCollider>();
        if(box)box.size=new Vector3(size.x,box.size.y,size.y);
    }
    static void CaptureAPlantZone(string phase)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ConstructionMapAuthoring.MapPath));
        map.GetComponent<MapInfo>().SetBarriersActive(false);ConstructionMapSurvey.Lighting();
        var environment=map.GetComponent<ConstructionMapEnvironment>();
        try{
            environment.Apply();
            ConstructionMapSurvey.Capture("A-plant-"+phase+"-top",new Vector3(-29,65,18),new Vector3(-29,0,18),1600,1200,20);
            ConstructionMapSurvey.Capture("A-plant-"+phase+"-angle",new Vector3(-47,30,-2),new Vector3(-29,0,18),1600,1000);
        }
        finally{environment.Restore();}
    }
    public static void ExpandAPlantZoneAndVerify()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only; preserve user's open scene");
        var previousPipeline=QualitySettings.renderPipeline;int previousAA=QualitySettings.antiAliasing;
        var paths=new[]{ConstructionMapAuthoring.MapPath,PresentationPath};
        var loaded=new GameObject[paths.Length];
        try{
            CaptureAPlantZone("before");
            // Edit the existing prefab objects in place, preserving file IDs and scene overrides.
            // Do not rebuild architecture, covers, B, spawns, barriers or the presentation hierarchy.
            for(int i=0;i<paths.Length;i++)loaded[i]=PrefabUtility.LoadPrefabContents(paths[i]);
            foreach(var map in loaded)ResizeAPlantZone(map);
            for(int i=0;i<paths.Length;i++)PrefabUtility.SaveAsPrefabAsset(loaded[i],paths[i]);
            foreach(var map in loaded)PrefabUtility.UnloadPrefabContents(map);
            Array.Clear(loaded,0,loaded.Length);
            CaptureAPlantZone("after");
            ConstructionMapVerification.Run();PlantZoneExport.Run();
            Debug.Log("A_PLANT_EXPANSION_PASS A=14x14 center=(-29,17) north=24; geometry/B preserved");
        }
        finally{
            foreach(var map in loaded)if(map)PrefabUtility.UnloadPrefabContents(map);
            QualitySettings.renderPipeline=previousPipeline;QualitySettings.antiAliasing=previousAA;
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
