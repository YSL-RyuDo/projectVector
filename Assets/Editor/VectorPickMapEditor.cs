using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class VectorPickMapEditor
{
    const string ScenePath = "Assets/Scenes/Game_TestVersion.unity";
    static T Ref<T>(Object owner, string field) where T : Object => new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
    static void SetRef(Object owner, string field, Object target)
    {
        var so = new SerializedObject(owner);
        so.FindProperty(field).objectReferenceValue = target;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: refused");
    }

    [MenuItem("Project Vector/UI/Install Authored Pick Map Background")]
    public static void InstallAndVerify()
    {
        Guard();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        // Clean only our managed preview instances from a previous installer pass.
        // Disabled MonoBehaviours can still run Awake, so a preview must not own a round singleton.
        var priorStage = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "PickMapStage");
        if (priorStage != null) RemoveRoundLogic(priorStage);
        var preserved = new VectorExperiencePolishEditor.Preservation(scene);
        var roots = scene.GetRootGameObjects();
        var pick = roots.SelectMany(r => r.GetComponentsInChildren<PickManager>(true)).Single();
        var match = roots.SelectMany(r => r.GetComponentsInChildren<MatchManager>(true)).Single();
        var panel = Ref<GameObject>(match, "playerPickPanel");
        var backdrop = panel.GetComponent<PickMapBackdrop>();
        if (backdrop == null) backdrop = panel.AddComponent<PickMapBackdrop>();
        var root = roots.FirstOrDefault(r => r.name == "PickMapStage") ?? new GameObject("PickMapStage");
        backdrop.stageRoot = root;
        backdrop.maps = new GameObject[2];
        root.transform.position = Vector3.zero;
        var origin = Ref<Transform>(pick, "previewSpawnPoint").position;
        for (int i = 0; i < 2; i++)
        {
            string name = i == 0 ? "TestMap" : "Desert";
            var map = root.transform.Find(name)?.gameObject;
            if (map == null)
            {
                map = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/" + name + ".prefab"), root.transform);
                map.name = name;
                VectorVictorySequenceEditor.Isolate(map);
                var info = map.GetComponent<MapInfo>();
                var point = info.attackSpawnPoints[0];
                map.transform.localRotation = Quaternion.Inverse(point.rotation);
                map.transform.position += origin - (point.position + point.forward * 4);
                if (info.barriers != null) info.barriers.SetActive(false);
            }
            RemoveRoundLogic(map);
            backdrop.maps[i] = map;
        }
        // Keep the existing preview camera, model spawn and gameplay-camera contracts.
        // Layer 31 is presentation-only and the stage is hidden synchronously with PlayerPick.
        var camera = roots.Single(r => r.name == "Main Camera").GetComponent<Camera>();
        camera.cullingMask |= 1 << 31;
        var floor = roots.Single(r => r.name == "Floor");
        foreach (var renderer in floor.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        SetRef(pick, "mapBackdrop", backdrop);
        backdrop.ApplyMap("TestMap");
        EditorUtility.SetDirty(backdrop);
        preserved.Check();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Pick scene save failed");
        AssetDatabase.SaveAssets();
        Verify();
    }

    static void RemoveRoundLogic(GameObject preview)
    {
        foreach (var round in preview.GetComponentsInChildren<RoundManager>(true)) Object.DestroyImmediate(round);
    }

    public static void Verify()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var roots = scene.GetRootGameObjects();
        var pick = roots.SelectMany(r => r.GetComponentsInChildren<PickManager>(true)).Single();
        var match = roots.SelectMany(r => r.GetComponentsInChildren<MatchManager>(true)).Single();
        var backdrop = Ref<PickMapBackdrop>(pick, "mapBackdrop");
        var panel = Ref<GameObject>(match, "playerPickPanel");
        foreach (var slot in panel.GetComponentsInChildren<TeamPickSlotUI>(true))
            typeof(TeamPickSlotUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(slot, null);
        var camera = roots.Single(r => r.name == "Main Camera").GetComponent<Camera>();
        int Count() => scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<Transform>(true).Length);
        int before = Count();
        var originalCameraPosition = camera.transform.position;
        var originalCameraRotation = camera.transform.rotation;
        var originalCharacterSpawn = Ref<Transform>(pick, "previewSpawnPoint").position;
        for (int i = 0; i < 2; i++)
        {
            pick.OnRecvPickState(new PickStateData { mapName = i == 0 ? "TestMap" : "Desert" });
            if (backdrop.SelectedMap != i || !backdrop.stageRoot.activeSelf || !backdrop.maps[i].activeSelf || backdrop.maps[1 - i].activeSelf)
                throw new Exception("Server pick map binding failed: " + i);
            foreach (var map in backdrop.maps)
            {
                if (map.GetComponentsInChildren<RoundManager>(true).Length != 0)
                    throw new Exception("Preview map can overwrite RoundManager.Instance in Awake");
                if (map.GetComponentsInChildren<Collider>(true).Any(c => c.enabled) ||
                    map.GetComponentsInChildren<MonoBehaviour>(true).Any(c => c != null && c.enabled) ||
                    map.GetComponentsInChildren<Camera>(true).Any(c => c.enabled) ||
                    map.GetComponentsInChildren<AudioSource>(true).Any(c => c.enabled))
                    throw new Exception("Presentation map can affect gameplay");
            }
            pick.OnRecvPickTimer(PickPhase.PREPARE, 0);
            if (Ref<Button>(pick, "selectButton").interactable) throw new Exception("Prepare phase unlocked selection");
            panel.SetActive(false);
            // Edit-mode fixtures do not dispatch non-ExecuteAlways lifecycle callbacks.
            typeof(PickMapBackdrop).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(backdrop, null);
            if (backdrop.stageRoot.activeSelf) throw new Exception("Pick map leaked into gameplay");
            panel.SetActive(true);
            typeof(PickMapBackdrop).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(backdrop, null);
        }
        foreach (string unknown in new string[] { null, "", "UnknownMap" })
        {
            backdrop.ApplyMap(unknown);
            if (backdrop.stageRoot.activeSelf) throw new Exception("Unknown map silently displayed wrong map");
        }
        if (Count() != before || camera.transform.position != originalCameraPosition || camera.transform.rotation != originalCameraRotation ||
            Ref<Transform>(pick, "previewSpawnPoint").position != originalCharacterSpawn)
            throw new Exception("Map selection changed hierarchy/camera/spawn");
        CaptureFixtures(pick, backdrop, camera);
        Debug.Log($"PICK_MAP_VERIFIED: {before} authored objects; server TestMap/Desert, unknown map, pick/loading/gameplay visibility, model/skill/lock controls, preserved camera/spawn; no runtime layout creation.");
    }

    static void CaptureFixtures(PickManager pick, PickMapBackdrop backdrop, Camera camera)
    {
        // Diagnostic fixtures only. These objects and state are never saved into the scene.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Invoke(string method) => typeof(PickManager).GetMethod(method, flags).Invoke(pick, null);
        Invoke("InitTeamPickSlotsAsWaiting"); Invoke("InitCharacterButtons"); Invoke("InitSkillButtons");
        var canvas = backdrop.GetComponentInParent<Canvas>();
        foreach (Transform child in canvas.transform)
            if (child.name != "PlayerPick" && child.name != "InGameUi") child.gameObject.SetActive(false);
        var top = Ref<TopInfoUI>(pick, "topInfoUI");
        top.gameObject.SetActive(true);
        top.SetTimer(20);
        var characterModels = new SerializedObject(pick).FindProperty("characterModelPrefabs");
        for (int characterId = 0; characterId < 4; characterId++)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(characterModels.GetArrayElementAtIndex(characterId).objectReferenceValue, Ref<Transform>(pick, "previewSpawnPoint"));
            try
            {
                VectorVictorySequenceEditor.Isolate(model);
                var pickProperties = new SerializedObject(pick);
                model.transform.localPosition = pickProperties.FindProperty("previewLocalPosition").vector3Value;
                model.transform.localRotation = Quaternion.Euler(pickProperties.FindProperty("previewLocalEulerAngles").vector3Value);
                model.transform.localScale = pickProperties.FindProperty("previewLocalScale").vector3Value;
                foreach (var animator in model.GetComponentsInChildren<Animator>())
                {
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Rebind(); animator.Update(.5f);
                }
                typeof(PickManager).GetField("currentPreviewModel", flags).SetValue(pick, model);
                typeof(PickManager).GetField("currentPreviewCharacterId", flags).SetValue(pick, characterId);
                var state = new PickStateData { mapName = characterId % 2 == 0 ? "TestMap" : "Desert", roomId = 7, myTeam = RoomSlotTeam.A };
                state.members.Add(new PickTeamMemberData { playerId = 1, isMe = true, nickname = "VECTOR", slotIndex = 0, hoverCharacterId = characterId, lockedCharacterId = -1 });
                pick.OnRecvPickTimer(PickPhase.PICKING, 20);
                pick.OnRecvPickState(state);
                if (!Ref<Button>(pick, "selectButton").interactable) throw new Exception("Map backdrop blocked character selection");
                foreach (string key in new[] { "cButton", "qButton", "eButton", "xButton" }) Ref<Button>(pick, key).onClick.Invoke();
                VectorPickExperienceEditor.VerifySkillInteraction(pick, state);
                if (characterId == 0)
                {
                    foreach (string map in new[] { "TestMap", "Desert" })
                    {
                        state.mapName = map; pick.OnRecvPickState(state);
                        Capture(canvas, camera, "VectorPick-" + map + ".png");
                    }
                }
                state.members[0].isLocked = true; state.members[0].lockedCharacterId = characterId;
                pick.OnRecvPickState(state);
                if (Ref<Button>(pick, "selectButton").interactable) throw new Exception("Locked character became selectable");
                state.members[0].isLocked = false;
                state.members.Add(new PickTeamMemberData { playerId = 2, nickname = "TEAM", slotIndex = 1, lockedCharacterId = characterId, isLocked = true });
                pick.OnRecvPickState(state);
                if (Ref<Button>(pick, "selectButton").interactable) throw new Exception("Teammate duplicate lock allowed");
                pick.OnRecvPickTimer(PickPhase.PREPARE, 0);
                if (Ref<Button>(pick, "selectButton").interactable) throw new Exception("Prepare unlocked pick");
                pick.OnRecvPickTimer(PickPhase.COMPLETE, 0);
                if (Ref<Button>(pick, "selectButton").interactable) throw new Exception("Complete unlocked pick");
            }
            finally
            {
                typeof(PickManager).GetField("currentPreviewModel", flags).SetValue(pick, null);
                typeof(PickManager).GetField("currentInfoCharacterId", flags).SetValue(pick, -1);
                typeof(PickManager).GetField("currentState", flags).SetValue(pick, null);
                Object.DestroyImmediate(model);
            }
        }
    }

    internal static void Capture(Canvas canvas, Camera camera, string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var scroll in canvas.GetComponentsInChildren<ScrollRect>())
            typeof(ScrollRect).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scroll, null);
        var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
        var quality = QualitySettings.renderPipeline;
        var oldTarget = camera.targetTexture;
        float oldAspect = camera.aspect;
        var target = new RenderTexture(1920, 1080, 24);
        var background = new GameObject("DiagnosticPickBackground", typeof(RectTransform), typeof(RawImage));
        try
        {
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            camera.targetTexture = target; camera.aspect = 16f / 9; camera.Render();
            var rect = background.GetComponent<RectTransform>(); rect.SetParent(canvas.transform, false); rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            background.layer = 5; var image = background.GetComponent<RawImage>(); image.texture = target; image.raycastTarget = false;
            VectorExperiencePolishEditor.CaptureCanvas(canvas, name);
        }
        finally
        {
            camera.targetTexture = oldTarget; camera.aspect = oldAspect;
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = quality;
            Object.DestroyImmediate(background); Object.DestroyImmediate(target);
        }
    }
    public static void Inspect()
    {
        Guard();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var t in all)
        {
            var c = t.GetComponent<Camera>();
            var im = t.GetComponent<Image>();
            var cv = t.GetComponent<Canvas>();
            if (c != null)
                Debug.Log($"PICK_CAMERA {Path(t)} pos={t.position} rot={t.eulerAngles} mask={c.cullingMask} enabled={c.enabled} rt={c.targetTexture}");
            if (im != null && (t.name.Contains("Background") || t.parent == null || cv != null))
                Debug.Log($"PICK_IMAGE {Path(t)} color={im.color} sprite={(im.sprite != null ? im.sprite.name : "none")} size={im.rectTransform.sizeDelta}");
            if (cv != null)
                Debug.Log($"PICK_CANVAS {Path(t)} active={t.gameObject.activeSelf} mode={cv.renderMode} sort={cv.sortingOrder}");
            if (t.parent == null) Debug.Log($"PICK_ROOT {t.name} active={t.gameObject.activeSelf} pos={t.position}");
        }
        foreach (var script in all.Select(t => t.GetComponent<MonoBehaviour>()).Where(s => s != null && (s is PickManager || s.GetType().Name == "MatchManager")))
        {
            var p = new SerializedObject(script).GetIterator();
            while (p.NextVisible(true))
                if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue is Component component && component != null)
                    Debug.Log($"PICK_REF {script.GetType().Name}.{p.propertyPath} = {Path(component.transform)}");
                else if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue is GameObject go && go != null)
                    Debug.Log($"PICK_REF {script.GetType().Name}.{p.propertyPath} = {Path(go.transform)}");
        }
    }
}
