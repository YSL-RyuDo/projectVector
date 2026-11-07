#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class MovementAimMapVerification
{
    static readonly string Output = Path.Combine(Path.GetTempPath(), "VectorMovementMapVerification-20261015");
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object Get(object target, string field) => target.GetType().GetField(field, Flags).GetValue(target);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Flags).Invoke(target, args);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static void AuthorAndVerify()
    {
        for (int layer = 0; layer < 32; layer++)
            Check(Physics.GetIgnoreLayerCollision(0, layer) == Physics.GetIgnoreLayerCollision(3, layer), "Map layer collision rules differ");
        AuthorMapReferences();
        Run();
    }

    [MenuItem("Tools/Vector/Repair Authored Map Minimap References")]
    public static void AuthorMapReferences()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play mode before authoring");
        Directory.CreateDirectory(Output);
        var report = new StringBuilder();
        foreach (string map in new[] { "TestMap", "Desert" })
        {
            string path = "Assets/ProjectVector/Shared/Maps/Prefabs/" + map + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var info = root.GetComponent<MapInfo>();
                var renderers = root.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => info.barriers == null || !r.transform.IsChildOf(info.barriers.transform)).ToArray();
                // Only authored static map meshes, never player/effect layers or round barriers.
                if (map == "Desert")
                    foreach (var renderer in renderers)
                        if (renderer.gameObject.layer == 0) renderer.gameObject.layer = LayerMask.NameToLayer("MiniMap");
                var refs = root.GetComponentInChildren<MiniMapWorldReferences>(true);
                if (refs == null)
                {
                    var go = new GameObject("MiniMapWorldReferences");
                    go.transform.SetParent(root.transform, false);
                    refs = go.AddComponent<MiniMapWorldReferences>();
                }
                var serialized = new SerializedObject(refs);
                if (map == "Desert")
                {
                    // Exclude oversized decorative sand sheets when fitting the playable footprint.
                    var footprint = renderers.Where(r => r.enabled && r.bounds.size.x < 150f && r.bounds.size.z < 150f &&
                        r.name.IndexOf("rock", StringComparison.OrdinalIgnoreCase) < 0).ToArray();
                    Bounds bounds = footprint[0].bounds;
                    foreach (var renderer in footprint.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    var center = refs.transform.Find("MiniMapFullCenter");
                    if (center == null) { center = new GameObject("MiniMapFullCenter").transform; center.SetParent(refs.transform, false); }
                    center.position = bounds.center;
                    serialized.FindProperty("fullMapCenter").objectReferenceValue = center;
                    foreach (var zone in root.GetComponentsInChildren<SpikePlantZone>(true))
                    {
                        if (zone.name.EndsWith("A")) serialized.FindProperty("zoneAPosition").objectReferenceValue = zone.transform;
                        if (zone.name.EndsWith("B")) serialized.FindProperty("zoneBPosition").objectReferenceValue = zone.transform;
                    }
                    serialized.FindProperty("fullMapSize").floatValue = Mathf.Max(bounds.extents.x, bounds.extents.z) + 5f;
                    report.AppendLine($"Desert playable bounds: {bounds}; overlay size={Mathf.Max(bounds.extents.x, bounds.extents.z) + 5f}");
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Check(Get(refs, "fullMapCenter") != null && Get(refs, "zoneAPosition") != null && Get(refs, "zoneBPosition") != null, map + " references incomplete");
                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.AppendLine(map + " saved with authored minimap references");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Path.Combine(Output, "authoring.txt"), report.ToString());
    }

    public static void Run()
    {
        Check(Application.isBatchMode, "Run this isolated verification through Unity -batchmode; it opens temporary scenes");
        Directory.CreateDirectory(Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var report = new StringBuilder();
        try
        {
            VerifyMovement(report);
            VerifyMaps(report);
            report.AppendLine("ALL CHECKS PASSED (isolated editor physics/render checks; not a multiplayer playtest)");
        }
        finally { File.WriteAllText(Path.Combine(Output, "verification.txt"), report.ToString()); }
    }

    static GameObject Box(string name, Vector3 position, Vector3 size)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name; box.transform.position = position; box.transform.localScale = size;
        return box;
    }

    static void VerifyMovement(StringBuilder report)
    {
        var floor = Box("VerificationFloor", new Vector3(0, -0.5f, 0), new Vector3(20, 1, 20));
        var keyboard = InputSystem.AddDevice<Keyboard>();
        foreach (string role in new[] { "Initiator", "Controller", "Sentinel", "Entry" })
        {
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Characters/Prefabs/" + role + "/" + role + ".prefab"));
            var movement = actor.GetComponent<PlayerMovement>();
            var cc = actor.GetComponent<CharacterController>();
            foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) if (collider != cc) collider.enabled = false;
            Call(movement, "Awake"); Call(movement, "Start");
            float scale = actor.transform.lossyScale.y;
            float height = cc.height;
            float bottom = cc.center.y - height * 0.5f;
            actor.transform.position = new Vector3(0, -bottom * scale + 0.02f, 0);
            Physics.SyncTransforms();
            Call(movement, "MoveAndResolveCeiling", Vector3.down * 0.2f);
            Check(cc.isGrounded, role + " failed to ground");
            Check((bool)Call(movement, "TrySetCrouching", true), "crouch rejected");
            Check(Mathf.Abs((height - cc.height) * scale - 0.5f) < 0.01f, role + " wrong capsule crouch drop");
            Check(Mathf.Abs(cc.center.y - cc.height * 0.5f - bottom) < 0.001f, role + " feet moved");
            Check(Mathf.Abs((((Vector3)Get(movement, "_standingPos")) - ((Vector3)Get(movement, "_crouchingPos"))).y * scale - 0.5f) < 0.01f, role + " wrong eye drop");
            float lowRoofHeight = (height - 0.5f / scale) * scale + 0.15f;
            var roof = Box("LowRoof", new Vector3(0, lowRoofHeight + 0.1f, 0), new Vector3(3, 0.2f, 3));
            Physics.SyncTransforms();
            Check(!(bool)Call(movement, "TrySetCrouching", false), role + " stood through ceiling");
            UnityEngine.Object.DestroyImmediate(roof);
            Physics.SyncTransforms();
            Check((bool)Call(movement, "TrySetCrouching", false), role + " failed to stand in open space");
            roof = Box("JumpCeiling", new Vector3(0, height * scale + 0.35f, 0), new Vector3(3, 0.2f, 3));
            Physics.SyncTransforms(); Set(movement, "_verticalVelocity", 5f);
            Call(movement, "MoveAndResolveCeiling", Vector3.up * 1f);
            Check((float)Get(movement, "_verticalVelocity") == 0f, role + " upward velocity survived ceiling hit");
            UnityEngine.Object.DestroyImmediate(roof);
            Call(movement, "MoveAndResolveCeiling", Vector3.down * 2f);
            var weaponGO = new GameObject("VerificationWeapon"); weaponGO.transform.SetParent(actor.transform);
            var weapon = weaponGO.AddComponent<Weapon>();
            var look = actor.GetComponentInChildren<FPSCameraLook>(true);
            var camera = actor.GetComponentInChildren<Camera>(true);
            Set(weapon, "playerMovement", movement); Set(weapon, "cameraLook", look);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.S)); InputSystem.Update();
            Check((Vector2)Call(movement, "getInput") == Vector2.zero, "opposite movement keys did not cancel");
            Check(!(bool)Call(weapon, "IsMoving"), "stationary grounded player received movement spread");
            Call(movement, "MoveAndResolveCeiling", Vector3.right * 0.1f + Vector3.down * 0.01f);
            Check((bool)Call(weapon, "IsMoving"), "actual movement did not affect spread");
            var wall = Box("BlockingWall", new Vector3(actor.transform.position.x + 0.7f, 1.5f, 0), new Vector3(1, 3, 3));
            Physics.SyncTransforms();
            for (int i = 0; i < 8; i++) Call(movement, "MoveAndResolveCeiling", Vector3.right * 0.2f + Vector3.down * 0.01f);
            Check(movement.PlanarSpeed < 0.1f, role + " blocked wall still has planar velocity");
            UnityEngine.Object.DestroyImmediate(wall);
            Physics.SyncTransforms();
            float walkSpeed = (float)Call(movement, "GetPlanarMoveSpeed");
            Call(movement, "TrySetCrouching", true);
            float crouchSpeed = (float)Call(movement, "GetPlanarMoveSpeed");
            Check(walkSpeed > 0.1f && Mathf.Abs(crouchSpeed / walkSpeed - 0.5f) < 0.01f, role + " crouch speed not applied");
            Call(movement, "TrySetCrouching", false);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            movement.SetADS(true); Set(look, "_isADS", true); Set(weapon, "isADS", true);
            Call(weapon, "OnDisable");
            Check(!weapon.IsAiming && !(bool)Get(movement, "isADS") && !(bool)Get(look, "_isADS"), "weapon switch left shared ADS state");
            Set(look, "isLocalPlayer", true); Set(look, "_isADS", false);
            camera.fieldOfView = 36; look.SetADSFromServer(true);
            Check(camera.fieldOfView == 36 && !(bool)Get(look, "_isADS"), "local ADS echo overrode weapon state");
            report.AppendLine($"PASS {role}: crouch capsule/feet/eye/speed, blocked standing, ceiling jump, opposite keys, actual movement/wall spread, ADS switch, local FOV echo");
            UnityEngine.Object.DestroyImmediate(actor);
        }
        InputSystem.RemoveDevice(keyboard);
        UnityEngine.Object.DestroyImmediate(floor);
    }

    static void VerifyMaps(StringBuilder report)
    {
        foreach (string map in new[] { "TestMap", "Desert" })
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/" + map + ".prefab"));
            // Match MatchManager.SpawnMap, not the prefab asset's stored root position.
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var refs = root.GetComponentInChildren<MiniMapWorldReferences>(true);
            Check(refs != null, map + " missing world references");
            var center = (Transform)Get(refs, "fullMapCenter");
            Check(center != null && Get(refs, "zoneAPosition") != null && Get(refs, "zoneBPosition") != null, "minimap references incomplete");
            var cameraGO = new GameObject("VerificationMiniMapCamera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.cullingMask = 1 << LayerMask.NameToLayer("MiniMap");
            camera.orthographic = true; camera.farClipPlane = 300; camera.aspect = 1;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            var initializationTarget = new RenderTexture(768, 768, 24);
            camera.targetTexture = initializationTarget;
            var controller = cameraGO.AddComponent<MiniMapCamera>();
            Call(controller, "Awake");
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(initializationTarget);
            controller.SetOverlayMapCenter(center);
            controller.SetMapFraming((float)Get(refs, "hudSize"), (float)Get(refs, "fullMapSize"), (float)Get(refs, "hudHeight"), (float)Get(refs, "fullMapHeight"));
            var info = root.GetComponent<MapInfo>();
            Physics.SyncTransforms();
            foreach (var point in info.attackSpawnPoints.Concat(info.defendSpawnPoints))
            {
                Check(Physics.Raycast(point.position + Vector3.up * 5, Vector3.down, out var ground, 30, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), map + " missing spawn floor");
                Check(ground.collider.transform.IsChildOf(root.transform), map + " spawn hits foreign collider");
            }
            var light = new GameObject("VerificationSun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1f; light.transform.rotation = Quaternion.Euler(65, -25, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.gray; RenderSettings.fog = false;
            controller.SetOverlayMode(true);
            foreach (byte team in new byte[] { 0, 1 })
            {
                controller.SetLocalRoundRole(team);
                foreach (var point in info.attackSpawnPoints.Concat(info.defendSpawnPoints))
                {
                    Vector3 uv = camera.WorldToViewportPoint(point.position);
                    Check(uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1 && uv.z > 0, map + " spawn outside full map frame");
                }
            }
            controller.SetLocalRoundRole(0);
            Capture(camera, map + "-overlay.png");
            controller.SetTarget(info.attackSpawnPoints[0]); controller.SetOverlayMode(false);
            Capture(camera, map + "-hud.png");
            int visibleGeometry = root.GetComponentsInChildren<Renderer>(true).Count(r => r.gameObject.layer == 3);
            Check(visibleGeometry > 15, map + " has insufficient visible minimap geometry");
            report.AppendLine($"PASS {map}: {visibleGeometry} minimap meshes, authored A/B/center, 6 spawn floor raycasts, both team overlay framing, HUD/overlay captures");
        }
    }

    static void Capture(Camera camera, string name)
    {
        var target = new RenderTexture(768, 768, 24);
        var texture = new Texture2D(768, 768, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 768, 768), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Output, name), texture.EncodeToPNG());
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(target); }
    }

    public static void InspectMaps()
    {
        Directory.CreateDirectory(Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var report = new StringBuilder();
        foreach (string map in new[] { "TestMap", "Desert" })
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/ProjectVector/Shared/Maps/Prefabs/" + map + ".prefab");
            try
            {
                report.AppendLine("MAP " + map);
                foreach (var group in root.GetComponentsInChildren<Renderer>(true).GroupBy(r => r.gameObject.layer))
                    report.AppendLine($"Layer {group.Key} {LayerMask.LayerToName(group.Key)}: {group.Count()} renderers");
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    report.AppendLine($"RENDER {AnimationUtility.CalculateTransformPath(renderer.transform, root.transform)} layer={renderer.gameObject.layer} active={renderer.gameObject.activeInHierarchy} bounds={renderer.bounds}");
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null) report.AppendLine($"SCRIPT {component.GetType().Name} {component.name}");
                var refs = root.GetComponentInChildren<MiniMapWorldReferences>(true);
                if (refs != null)
                    foreach (string field in new[] { "zoneAPosition", "zoneBPosition", "fullMapCenter" })
                        report.AppendLine($"REF {field}={Get(refs, field)}");
                var info = root.GetComponentInChildren<MapInfo>(true);
                if (info != null)
                    foreach (var point in info.attackSpawnPoints.Concat(info.defendSpawnPoints))
                        if (point != null) report.AppendLine($"SPAWN {point.name} {point.position}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        File.WriteAllText(Path.Combine(Output, "map-inspection.txt"), report.ToString());
    }
}
#endif
