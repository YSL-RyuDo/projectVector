#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CityMapIntegration
{
    public const string SourcePath = "Assets/ProjectVector/Development/Maps/CityPrototype/City.prefab";
    public const string MapPath = "Assets/ProjectVector/Shared/Maps/Prefabs/City.prefab";
    public const string PresentationPath = "Assets/ProjectVector/Shared/Maps/Prefabs/CityPresentation.prefab";
    public static readonly string Output = Path.Combine(Path.GetTempPath(), "VectorCityMap");

    static T Ref<T>(Object owner, string field) where T : Object =>
        new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
    static void SetRef(Object owner, string field, Object value)
    {
        var serialized = new SerializedObject(owner);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static T[] All<T>() where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    public static void InstallAndVerify()
    {
        if (!Application.isBatchMode) throw new Exception("Batch only; preserve the user's open scenes");
        BuildGamePrefab();
        BuildPresentation();
        InstallLobby();
        InstallMatch();
        foreach (string path in new[] { "Assets/ProjectVector/Modules/Results/Scenes/Result.unity", "Assets/ProjectVector/Modules/Results/Scenes/RankResult.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            var stage = All<ResultCharacterStage>().SingleOrDefault();
            if (!stage) continue; // Legacy rank summary has no map stage.
            var preview = Preview(stage.maps[0].transform.parent, stage.transform.position);
            Array.Resize(ref stage.maps, Math.Max(4, stage.maps.Length)); stage.maps[3] = preview;
            stage.SelectMap("TestMap"); EditorUtility.SetDirty(stage); EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        VerifyAndExport();
        Debug.Log("CITY_INTEGRATION_PASS gameplay, private selection, pick intro, result, minimap, authoritative geometry");
    }

    public static void VerifyAndExport()
    {
        CityMapVerification.Run();
        PlantZoneExport.Run(); MovementSpawnExport.Run(); ScannerOcclusionExport.Run();
    }

    static void BuildGamePrefab()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // A variant keeps SungHyun's map, spawn ordering, sites and future source updates linked.
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(File.Exists(MapPath) ? MapPath : SourcePath);
        var map = (GameObject)PrefabUtility.InstantiatePrefab(source);
        map.name = "City";
        var sites = map.GetComponentsInChildren<SpikePlantZone>(true);
        if (sites.Length != 2) throw new Exception("City must retain its two authored plant zones");
        var all = map.GetComponentsInChildren<Transform>(true);
        var a = all.Single(t => t.name == "ASite"); var b = all.Single(t => t.name == "BSite");
        var center = map.transform.Find("MiniMapFullCenter");
        if (!center) { center = new GameObject("MiniMapFullCenter").transform; center.SetParent(map.transform, false); }
        var references = map.GetComponent<MiniMapWorldReferences>() ?? map.AddComponent<MiniMapWorldReferences>();
        SetRef(references, "zoneAPosition", a); SetRef(references, "zoneBPosition", b); SetRef(references, "fullMapCenter", center);
        var serialized = new SerializedObject(references);
        serialized.FindProperty("hudSize").floatValue = 28;
        serialized.FindProperty("fullMapSize").floatValue = 52;
        serialized.FindProperty("hudHeight").floatValue = 70;
        serialized.FindProperty("fullMapHeight").floatValue = 120;
        // City geometry uses Ground. Include it only in this map's minimap; keep all collision layers intact.
        serialized.FindProperty("additionalVisibleLayers").intValue = LayerMask.GetMask("Ground");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(map, MapPath); Object.DestroyImmediate(map);
    }

    static void BuildPresentation()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var map = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MapPath));
        PrefabUtility.UnpackPrefabInstance(map, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        map.GetComponent<MapInfo>().SetBarriersActive(false);
        foreach (var component in map.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(component);
        foreach (var component in map.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(component);
        foreach (var transform in map.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 31;
        PrefabUtility.SaveAsPrefabAsset(map, PresentationPath); Object.DestroyImmediate(map);
    }

    static GameObject Preview(Transform parent, Vector3 origin)
    {
        var existing = parent.Find("City"); if (existing) Object.DestroyImmediate(existing.gameObject);
        var preview = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PresentationPath), parent);
        preview.name = "City";
        var spawn = AssetDatabase.LoadAssetAtPath<GameObject>(MapPath).GetComponent<MapInfo>().attackSpawnPoints[0];
        preview.transform.rotation = Quaternion.Inverse(spawn.rotation);
        preview.transform.position = origin - preview.transform.rotation * (spawn.position + spawn.forward * 4);
        return preview;
    }

    static void InstallLobby()
    {
        var scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var rules = All<RoomRulesPanelUI>().Single();
        var buttons = new[] { Ref<Button>(rules, "testMapButton"), Ref<Button>(rules, "desertButton"), Ref<Button>(rules, "constructionButton") };
        var city = Ref<Button>(rules, "cityButton");
        if (!city)
        {
            city = Object.Instantiate(buttons[2], buttons[2].transform.parent);
            city.name = "CityMapButton"; city.onClick = new Button.ButtonClickedEvent();
            var rects = buttons.Select(button => (RectTransform)button.transform).ToArray();
            bool horizontal = Mathf.Abs(rects[2].anchoredPosition.x - rects[0].anchoredPosition.x) >= Mathf.Abs(rects[2].anchoredPosition.y - rects[0].anchoredPosition.y);
            var all = rects.Append((RectTransform)city.transform).ToArray();
            if (horizontal)
            {
                float left = rects.Min(r => r.anchoredPosition.x - r.sizeDelta.x / 2), right = rects.Max(r => r.anchoredPosition.x + r.sizeDelta.x / 2);
                float width = (right - left - 36) / 4;
                for (int i = 0; i < all.Length; i++) { all[i].sizeDelta = new Vector2(width, all[i].sizeDelta.y); all[i].anchoredPosition = new Vector2(left + width / 2 + i * (width + 12), rects[0].anchoredPosition.y); }
            }
            else
            {
                float top = rects.Max(r => r.anchoredPosition.y + r.sizeDelta.y / 2), bottom = rects.Min(r => r.anchoredPosition.y - r.sizeDelta.y / 2);
                float height = (top - bottom - 30) / 4;
                for (int i = 0; i < all.Length; i++) { all[i].sizeDelta = new Vector2(all[i].sizeDelta.x, height); all[i].anchoredPosition = new Vector2(rects[0].anchoredPosition.x, top - height / 2 - i * (height + 10)); }
            }
            SetRef(rules, "cityButton", city);
        }
        foreach (var text in city.GetComponentsInChildren<TMP_Text>(true)) { text.text = "도시"; text.enableAutoSizing = true; text.fontSizeMin = 15; }
        rules.ApplySelectedMap("TestMap"); EditorSceneManager.SaveScene(scene);
    }

    static void InstallMatch()
    {
        var scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var match = All<MatchManager>().Single(); var serialized = new SerializedObject(match); var maps = serialized.FindProperty("maps");
        var city = AssetDatabase.LoadAssetAtPath<GameObject>(MapPath);
        int cityIndex = -1;
        for (int i = 0; i < maps.arraySize; i++)
        {
            var candidate = maps.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (candidate && candidate.name == "City") { cityIndex = i; break; }
        }
        if (cityIndex < 0) { cityIndex = maps.arraySize; maps.InsertArrayElementAtIndex(cityIndex); }
        maps.GetArrayElementAtIndex(cityIndex).objectReferenceValue = city;
        for (int i = maps.arraySize - 1; i > cityIndex; i--)
        {
            var candidate = maps.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (!candidate || candidate.name != "City") continue;
            maps.GetArrayElementAtIndex(i).objectReferenceValue = null; maps.DeleteArrayElementAtIndex(i);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var backdrop = All<PickMapBackdrop>().Single(); var pick = All<PickManager>().Single();
        var preview = Preview(backdrop.stageRoot.transform, Ref<Transform>(pick, "previewSpawnPoint").position);
        Array.Resize(ref backdrop.maps, Math.Max(4, backdrop.maps.Length)); backdrop.maps[3] = preview;
        var intro = All<MapIntroPresentation>().Single();
        Array.Resize(ref intro.startPoses, Math.Max(4, intro.startPoses.Length)); Array.Resize(ref intro.endPoses, Math.Max(4, intro.endPoses.Length));
        for (int i = 0; i < 2; i++)
        {
            var pose = new GameObject(i == 0 ? "CityIntroStart" : "CityIntroEnd").transform; pose.SetParent(preview.transform, false);
            pose.localPosition = i == 0 ? new Vector3(58, 56, 58) : new Vector3(35, 32, 40);
            pose.rotation = Quaternion.LookRotation(preview.transform.TransformPoint(Vector3.zero) - pose.position);
            if (i == 0) intro.startPoses[3] = pose; else intro.endPoses[3] = pose;
        }
        backdrop.ApplyMap("TestMap"); EditorUtility.SetDirty(backdrop); EditorUtility.SetDirty(intro); EditorSceneManager.SaveScene(scene);
    }

    public static void AuditSource()
    {
        if (!Application.isBatchMode) throw new Exception("Batch only; preserve the user's open scenes");
        Directory.CreateDirectory(Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var map = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath));
        map.GetComponent<MapInfo>().SetBarriersActive(false);
        Physics.SyncTransforms();
        var report = new StringBuilder();
        foreach (var t in map.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains("Spawn") || t.name.StartsWith("A ") || t.name.StartsWith("B ") || t.name.Contains("arrier"))
                report.AppendLine(t.name + " world=" + t.position.ToString("F3"));
        }
        var info = map.GetComponent<MapInfo>();
        foreach (var point in info.attackSpawnPoints.Concat(info.defendSpawnPoints).Append(info.spikeSpawnPoint))
        {
            bool grounded = Physics.Raycast(point.position + Vector3.up * .3f, Vector3.down, out var hit, 5, ~0, QueryTriggerInteraction.Ignore);
            report.AppendLine("SPAWN " + point.name + " " + point.position.ToString("F3") + " yaw=" + point.eulerAngles.y + " ground=" + grounded + " hit=" + (grounded ? hit.point.ToString("F3") + " " + hit.collider.name : "none"));
        }
        File.WriteAllText(Path.Combine(Output, "source-audit.txt"), report.ToString());
        Capture(map, "city-source-top", new Vector3(0, 100, 0), Quaternion.Euler(90, 0, 0), true);
        Capture(map, "city-source-overview", new Vector3(60, 65, 65), Quaternion.LookRotation(new Vector3(0, 0, 0) - new Vector3(60, 65, 65)), false);
        Object.DestroyImmediate(map);
        Debug.Log("CITY_SOURCE_AUDIT_PASS " + Output);
    }

    public static void Capture(GameObject map, string name, Vector3 position, Quaternion rotation, bool orthographic)
    {
        Directory.CreateDirectory(Output);
        var lightObject = new GameObject("City verification light", typeof(Light));
        var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f;
        light.transform.rotation = Quaternion.Euler(55, -25, 0);
        var cameraObject = new GameObject("City verification camera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.transform.SetPositionAndRotation(position, rotation);
        camera.orthographic = orthographic; camera.orthographicSize = 52; camera.fieldOfView = 55;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.20f, .24f, .28f);
        camera.nearClipPlane = .1f; camera.farClipPlane = 500;
        var target = new RenderTexture(1600, 1200, 24); target.Create(); camera.targetTexture = target;
        var previous = RenderTexture.active;
        try
        {
            camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG()); Object.DestroyImmediate(image);
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            Object.DestroyImmediate(target); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(lightObject);
        }
    }
}
#endif
