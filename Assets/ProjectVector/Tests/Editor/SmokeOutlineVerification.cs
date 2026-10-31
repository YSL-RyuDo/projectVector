#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SmokeOutlineVerification
{
    const string Key = "Vector.SmokeOutlineTest";
    const string Prefab = "Assets/ProjectVector/Shared/Characters/Effects/Entry/LegacySmoke/Smoke Effect.prefab";
    static double deadline;
    static bool ran, passed;
    static System.Collections.IEnumerator revealChecks;
    static Camera camera;
    static GameObject target;
    static PlayerOutline player;
    static Outline outline;
    static Renderer mesh;
    static int checks;

    static SmokeOutlineVerification()
    {
        EditorApplication.playModeStateChanged += State;
        if (SessionState.GetBool(Key, false)) { EditorApplication.update += Tick; deadline = EditorApplication.timeSinceStartup + 90; }
    }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Batch only; never replaces an interactive scene");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Key, true);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.EnterPlaymode();
    }
    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Key, false); EditorApplication.Exit(passed ? 0 : 1); }
    }
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("SMOKE_FAIL " + label);
        checks++; Debug.Log("SMOKE_CHECK " + label);
    }
    static GameObject Smoke(Vector3 position, float radius = 1)
    {
        var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), position, Quaternion.identity);
        go.transform.localScale = Vector3.one * radius;
        Check(go.GetComponent<SmokeOutlineOccluder>() != null, "saved prefab has visibility volume");
        return go;
    }
    static void RenderCheck(bool hidden, string label)
    {
        camera.transform.LookAt(target.transform.position + Vector3.up);
        camera.Render(); // Real pipeline callback, not direct invocation of visibility code.
        Check(outline.SmokeSuppressed == hidden, label + " / camera suppression");
        Check(mesh.sharedMaterials.Count(m => m && m.name.StartsWith("Outline")) == (hidden ? 0 : 2), label + " / render passes");
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (!EditorApplication.isPlaying) { if (EditorApplication.timeSinceStartup > deadline) EditorApplication.Exit(1); return; }
        try {
            if (!ran) { ran = true; Verify(); revealChecks = SmokeSkillRevealVerification.Verify(); }
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Smoke reveal verification timed out");
            if (revealChecks.MoveNext()) return;
            passed = true; Debug.Log("SMOKE_OUTLINE_PASS checks=" + checks);
        }
        catch (Exception ex) { Debug.LogException(ex); passed = false; (revealChecks as IDisposable)?.Dispose(); }
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }
    static void Verify()
    {
        camera = new GameObject("VerificationCamera").AddComponent<Camera>();
        camera.enabled = false;
        camera.transform.position = new Vector3(0, 1, -10);
        camera.targetTexture = new RenderTexture(256, 256, 24);
        target = new GameObject("OutlineTarget");
        var capsule = target.AddComponent<CharacterController>(); capsule.center = Vector3.up; capsule.height = 2; capsule.radius = .3f;
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.transform.SetParent(target.transform, false); body.transform.localPosition = Vector3.up;
        mesh = body.GetComponent<Renderer>();
        player = target.AddComponent<PlayerOutline>(); outline = target.AddComponent<Outline>(); player.Configure(false, false);
        RenderCheck(false, "clear enemy");
        var smoke = Smoke(new Vector3(0, 1, -5));
        RenderCheck(true, "both outside, smoke between viewer and enemy");
        player.Configure(false, true); RenderCheck(false, "ally stays identifiable through smoke");
        smoke.transform.position = Vector3.up; RenderCheck(false, "ally inside smoke stays identifiable");
        smoke.transform.position = camera.transform.position; RenderCheck(false, "viewer in smoke still identifies ally");
        player.Configure(false, false);
        smoke.transform.position = Vector3.up; RenderCheck(true, "target inside smoke");
        smoke.transform.position = new Vector3(1.2f, 1, 0); RenderCheck(true, "partial body overlap");
        smoke.transform.position = camera.transform.position; RenderCheck(true, "viewer inside smoke");
        smoke.transform.position = new Vector3(0, 1, -12); RenderCheck(false, "smoke behind camera");
        smoke.transform.position = new Vector3(0, 1, 3); RenderCheck(false, "smoke beyond target");
        smoke.transform.position = new Vector3(4, 1, -5); RenderCheck(false, "off-axis smoke");
        smoke.transform.position = Vector3.up; smoke.transform.localScale = Vector3.one * 4.5f;
        camera.transform.position = new Vector3(0, 1, -2);
        player.Configure(false, false); RenderCheck(false, "same smoke interior shows enemy outline");
        Check(outline.OutlineMode == Outline.Mode.OutlineVisible, "normal enemy outline stays depth tested, not wall reveal");
        // Inspect the production material's wall-depth mode. No screenshots or video.
        outline.SendMessage("Update");
        var fill = mesh.sharedMaterials.Single(m => m && m.name.StartsWith("OutlineFill"));
        Check(fill.GetFloat("_ZTest") == (float)UnityEngine.Rendering.CompareFunction.LessEqual, "restored outline respects wall depth");
        player.Configure(false, true); RenderCheck(false, "same smoke interior shows ally outline");
        player.Configure(false, false);
        camera.transform.position = new Vector3(0, 1, -5); RenderCheck(true, "walking out hides inside enemy again");
        camera.transform.position = new Vector3(0, 1, -2); RenderCheck(false, "walking back in restores outline");
        target.transform.position = new Vector3(0, 0, 5); RenderCheck(true, "viewer inside target outside stays hidden");
        target.transform.position = new Vector3(0, 0, 4.3f); RenderCheck(true, "body straddling boundary stays hidden");
        target.transform.position = Vector3.zero;
        var interiorBlocker = Smoke(new Vector3(0, 1, -1), .4f);
        RenderCheck(true, "shared interior does not bypass a second smoke shell");
        Object.DestroyImmediate(interiorBlocker); RenderCheck(false, "second smoke removed restores shared interior");
        smoke.transform.localScale = Vector3.one; camera.transform.position = new Vector3(0, 1, -10);
        smoke.transform.position = new Vector3(0, 1, -5);
        outline.OutlineColor = Color.yellow; outline.OutlineWidth = 5; outline.enabled = false; outline.enabled = true;
        RenderCheck(true, "unconfirmed style changes cannot bypass smoke");
        smoke.SetActive(false); RenderCheck(false, "disable immediately restores render passes");
        Check(outline.OutlineColor == Color.yellow && outline.OutlineWidth == 5, "reveal style preserved");
        smoke.SetActive(true);
        var second = Smoke(new Vector3(0, 1, -3));
        Object.DestroyImmediate(smoke); RenderCheck(true, "destroy one overlapping smoke, second remains");
        var alternate = new GameObject("DroneOrSpectatorCamera").AddComponent<Camera>();
        alternate.enabled = false; alternate.targetTexture = camera.targetTexture;
        alternate.transform.position = new Vector3(10, 1, -10); alternate.transform.LookAt(Vector3.up);
        alternate.Render(); Check(!outline.SmokeSuppressed, "second camera has independent clear sightline");
        RenderCheck(true, "main camera restores suppression in same frame");
        Object.DestroyImmediate(alternate.gameObject);
        camera.transform.position = new Vector3(10, 1, -10); RenderCheck(false, "observer camera moves outside occlusion");
        camera.transform.position = new Vector3(0, 1, -10); RenderCheck(true, "return to occluded camera view");
        target.transform.position = new Vector3(10, 0, 0); RenderCheck(false, "target moves out of occluded sightline");
        target.transform.position = Vector3.zero;
        capsule.height = 1; capsule.center = Vector3.up * .5f; RenderCheck(true, "crouched body stays hidden");
        Object.DestroyImmediate(second); RenderCheck(false, "last smoke removed restores outline");
        player.Configure(true, true); camera.Render(); Check(!outline.enabled && mesh.sharedMaterials.Length == 1, "local player stays disabled");
        player.Configure(false, false); player.AddDreamForestHide(); camera.Render(); Check(!outline.enabled, "other concealment stays disabled"); player.RemoveDreamForestHide();

        Check(SmokeOutlineOccluder.IntersectsSegment(Vector3.zero, Vector3.zero, Vector3.zero, 1), "zero segment inside");
        Check(!SmokeOutlineOccluder.IntersectsSegment(Vector3.zero, Vector3.zero, Vector3.one * 3, 1), "zero segment outside");
        Check(SmokeOutlineOccluder.IntersectsSegment(Vector3.zero, Vector3.forward * 10, new Vector3(1, 0, 5), 1), "tangent boundary");
        foreach (string name in new[] { "TestMap", "Desert" })
        {
            var map = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/" + name + ".prefab"));
            var info = map.GetComponent<MapInfo>();
            foreach (var spawn in info.attackSpawnPoints.Concat(info.defendSpawnPoints))
            {
                target.transform.position = spawn.position;
                camera.transform.position = spawn.position + new Vector3(0, 1, -10);
                var s = Smoke(spawn.position + new Vector3(0, 1, -5), 3);
                Check(Mathf.Approximately(s.GetComponent<SmokeOutlineOccluder>().WorldRadius, 3), name + " server radius retained");
                RenderCheck(true, name + " spawn sightline");
                Object.DestroyImmediate(s); RenderCheck(false, name + " smoke removed");
                s = Smoke(spawn.position + Vector3.up, 4.5f);
                camera.transform.position = spawn.position + new Vector3(0, 1, -2);
                RenderCheck(false, name + " same smoke interior");
                camera.transform.position = spawn.position + new Vector3(0, 1, -6);
                RenderCheck(true, name + " exit crosses smoke boundary");
                Object.DestroyImmediate(s);
            }
            Object.DestroyImmediate(map);
        }
        foreach (string type in new[] { "Initiator", "Controller", "Sentinel", "Entry" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Characters/Prefabs/" + type + "/" + type + ".prefab");
            Check(prefab != null, type + " gameplay prefab exists");
            var outlines = prefab.GetComponentsInChildren<Outline>(true);
            Check(outlines.Length > 0 && outlines.All(o => o.GetComponentInParent<PlayerOutline>() || o.GetComponentInChildren<PlayerOutline>(true)), type + " real outline linked to player concealment");
        }
        Object.DestroyImmediate(camera.targetTexture);
        ControllerSmokeInteractionVerification.Verify();
    }
}
#endif
