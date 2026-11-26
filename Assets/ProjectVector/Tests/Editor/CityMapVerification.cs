#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CityMapVerification
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("CITY_VERIFY_FAIL " + message); checks++; }
    static T[] All<T>() where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    static T Ref<T>(Object owner, string name) where T : Object => new SerializedObject(owner).FindProperty(name).objectReferenceValue as T;
    static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Batch only");
        checks = 0; Directory.CreateDirectory(CityMapIntegration.Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(CityMapIntegration.SourcePath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CityMapIntegration.MapPath);
        Check(PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant, "Game map remains linked to source");
        var map = (GameObject)PrefabUtility.InstantiatePrefab(prefab); var info = map.GetComponent<MapInfo>();
        var original = source.GetComponent<MapInfo>();
        Check(info.attackSpawnPoints.Length == 3 && info.defendSpawnPoints.Length == 3, "Six team spawns");
        Check(info.deathmatchSpawnPoints.Length == original.deathmatchSpawnPoints.Length, "Deathmatch spawn order retained");
        Check(map.GetComponentsInChildren<Collider>(true).Length == source.GetComponentsInChildren<Collider>(true).Length, "Original collision geometry preserved");
        var sites = map.GetComponentsInChildren<SpikePlantZone>(true);
        Check(sites.Length == 2, "Two original plant zones retained");
        info.SetBarriersActive(false); Physics.SyncTransforms();
        var report = new StringBuilder();
        for (byte team = 0; team < 2; team++) for (byte slot = 0; slot < 3; slot++)
        {
            var point = info.GetTeamSlotSpawnPoint(team, slot); var expected = original.GetTeamSlotSpawnPoint(team, slot);
            Check(point && Vector3.Distance(point.position, expected.position) < .001f && Quaternion.Angle(point.rotation, expected.rotation) < .01f, "Original team spawn and facing");
            Check(Physics.Raycast(point.position + Vector3.up * .3f, Vector3.down, out var floor, 2, ~0, QueryTriggerInteraction.Ignore), "Spawn floor " + point.name);
            Check(!Physics.CheckCapsule(floor.point + Vector3.up * .35f, floor.point + Vector3.up * 1.5f, .3f, ~0, QueryTriggerInteraction.Ignore), "Standing capsule clear " + point.name);
            report.AppendLine("SPAWN " + team + " " + slot + " " + point.position.ToString("F4"));
        }
        foreach (var site in sites)
        {
            var box = site.GetComponent<BoxCollider>(); Check(box && box.enabled && box.isTrigger, "Active plant trigger");
            report.AppendLine("PLANT " + site.name + " " + box.bounds);
        }
        var references = map.GetComponent<MiniMapWorldReferences>();
        Check(references && Ref<Transform>(references, "zoneAPosition").name == "ASite" && Ref<Transform>(references, "zoneBPosition").name == "BSite", "Authored A/B markers retain their labels");
        Check(Ref<Transform>(references, "fullMapCenter") != null, "Minimap center saved");
        CityMapIntegration.Capture(map, "city-game-overview", new Vector3(60, 65, 65), Quaternion.LookRotation(-new Vector3(60, 65, 65)), false);
        File.WriteAllText(Path.Combine(CityMapIntegration.Output, "game-audit.txt"), report.ToString());
        Object.DestroyImmediate(map);

        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Lobby/Scenes/Lobby.unity");
        var rules = All<RoomRulesPanelUI>().Single(); var button = Ref<Button>(rules, "cityButton");
        Check(button && button.GetComponentInChildren<TMP_Text>(true).text == "도시", "Saved city selection button");
        GameMapId selected = GameMapId.TestMap; rules.OnMapSelected = value => selected = value; Invoke(rules, "Awake"); button.onClick.Invoke();
        Check(selected == GameMapId.City && (byte)selected == 3, "Button sends map ID 3");
        rules.ApplySelectedMap("City"); Check(Ref<TMP_Text>(rules, "selectedMapText").text.Contains("도시"), "Selected city label");
        Check(button.targetGraphic.color != Ref<Button>(rules, "testMapButton").targetGraphic.color, "Only city is selected");
        foreach (var field in new[] { "testMapButton", "desertButton", "constructionButton" })
        {
            var other = Ref<Button>(rules, field); Check(other != null, "Existing choice retained " + field);
            Check(!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)other.transform, RectTransformUtility.WorldToScreenPoint(null, button.transform.position)), "Map choices do not overlap " + field);
        }

        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var match = All<MatchManager>().Single(); var maps = new SerializedObject(match).FindProperty("maps");
        Check(maps.arraySize == 4, "Four gameplay map prefabs");
        Check(maps.GetArrayElementAtIndex(3).objectReferenceValue == prefab, "City resolves to playable variant");
        var backdrop = All<PickMapBackdrop>().Single();
        backdrop.gameObject.SetActive(true); backdrop.ApplyMap("City");
        Check(backdrop.SelectedMap == 3 && backdrop.maps[3].activeSelf, "City pick backdrop");
        Check(backdrop.maps[3].GetComponentsInChildren<Collider>(true).Length == 0 && backdrop.maps[3].GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "Presentation contains no gameplay logic/collisions");
        var intro = All<MapIntroPresentation>().Single();
        intro.Begin(new PickStateData { roomId = 904, mapName = "City" });
        Check(intro.IsPlaying && intro.mapTitle.text == "CITY" && intro.startPoses[3] && intro.endPoses[3], "City flyover references");
        intro.Evaluate(.5f); Check(Vector3.Distance(intro.overviewCamera.transform.position, intro.startPoses[3].position) > 1, "City camera moves");
        intro.Finish(); backdrop.ApplyMap("TestMap"); Check(!backdrop.maps[3].activeSelf && backdrop.maps[0].activeSelf, "Existing backdrop still selectable");
        Invoke(match, "SpawnMapIfNeeded", "City");
        var spawned = (MapInfo)typeof(MatchManager).GetField("currentMapInfo", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(match);
        Check(spawned && spawned.name.StartsWith("City") && spawned.GetComponent<MiniMapWorldReferences>(), "Real MatchManager loads the City variant without fallback");
        var minimap = All<MiniMapCamera>().Single(); Invoke(minimap, "Awake");
        int baseline = minimap.Camera.cullingMask;
        minimap.SetMapFraming(28, 52, 70, 120, LayerMask.GetMask("Ground"));
        Check((minimap.Camera.cullingMask & LayerMask.GetMask("Ground")) != 0, "City ground is visible on minimap");
        Check((minimap.Camera.cullingMask & LayerMask.GetMask("Enemy", "Player", "LocalPlayer")) == 0, "Minimap does not reveal players through geometry layers");
        minimap.SetMapFraming(30, 70, 50, 100); Check(minimap.Camera.cullingMask == baseline, "Next map restores camera layers");
        foreach (string scene in new[] { "Result", "RankResult" })
        {
            EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Results/Scenes/" + scene + ".unity");
            var stage = All<ResultCharacterStage>().SingleOrDefault();
            if (!stage) { Check(scene == "RankResult", "Only legacy rank summary omits a map stage"); continue; }
            stage.SelectMap("City");
            Check(stage.SelectedMap == 3 && stage.maps[3].activeSelf && !stage.maps[0].activeSelf, "City result background " + scene);
            stage.SelectMap("Desert"); Check(stage.maps[1].activeSelf && !stage.maps[3].activeSelf, "Existing result background " + scene);
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log("CITY_VERIFY_PASS " + checks + " checks");
    }
}
#endif
