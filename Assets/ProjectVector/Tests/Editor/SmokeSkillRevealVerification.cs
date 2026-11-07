#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Isolated play-mode tests, driven by SmokeOutlineVerification.Run.
public static class SmokeSkillRevealVerification
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string label)
    {
        if (!value) throw new Exception("SMOKE_REVEAL_FAIL " + label);
        Debug.Log("SMOKE_REVEAL_CHECK " + label);
    }
    static byte[] Packet(uint source, uint target, float duration)
    {
        var bytes = new byte[12];
        Buffer.BlockCopy(BitConverter.GetBytes(source), 0, bytes, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(target), 0, bytes, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(duration), 0, bytes, 8, 4);
        return bytes;
    }
    static Color32[] Render(Camera camera, string file = null)
    {
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;
        var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); image.Apply();
        var pixels = image.GetPixels32();
        if (file != null) File.WriteAllBytes(file, image.EncodeToPNG());
        Object.DestroyImmediate(image); RenderTexture.active = previous;
        return pixels;
    }
    static int YellowPixels(Color32[] pixels) => pixels.Count(c => c.r > 160 && c.g > 160 && c.b < 100);

    public static IEnumerator Verify()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
        var output = Environment.GetEnvironmentVariable("VECTOR_SMOKE_REVEAL_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "VectorSmokeReveal");
        Directory.CreateDirectory(output);
        var network = new GameObject("NetworkFixture_NoConnection").AddComponent<NetworkManager>();
        network.enabled = false; // Start must never open a real connection in this fixture.
        typeof(NetworkManager).GetField("myPlayerId", Fields).SetValue(network, (uint)10);
        var local = new GameObject("CasterTeamViewer").AddComponent<PlayerIdentity>(); local.PlayerId = 10; local.Team = 0;
        var receiver = new GameObject("ProductionRevealReceiver").AddComponent<SentinelCNetworkManager>();
        var registry = (Dictionary<uint, Sentinel_Trap>)typeof(SentinelCNetworkManager).GetField("traps", Fields).GetValue(receiver);
        Sentinel_Trap Source(uint id)
        {
            var trap = new GameObject("RevealSource" + id).AddComponent<Sentinel_Trap>();
            trap.InitNetwork(id, 11, 0, false, 40); registry.Add(id, trap); return trap;
        }
        var source = Source(1); var other = Source(2);
        var target = GameObject.CreatePrimitive(PrimitiveType.Capsule); target.name = "DetectedEnemy"; target.transform.position = Vector3.up;
        var identity = target.AddComponent<PlayerIdentity>(); identity.PlayerId = 20; identity.Team = 1;
        var health = target.AddComponent<PlayerHP>();
        var capsule = target.AddComponent<CharacterController>(); capsule.height = 2; capsule.radius = .5f;
        var body = target.GetComponent<Renderer>(); body.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        body.sharedMaterial.color = Color.gray;
        var player = target.AddComponent<PlayerOutline>(); var outline = target.AddComponent<Outline>(); player.Configure(false, false);
        Color normalColor = outline.OutlineColor; float normalWidth = outline.OutlineWidth;
        var camera = new GameObject("ActualSmokeRenderCamera").AddComponent<Camera>(); camera.enabled = false;
        camera.transform.position = new Vector3(0, 1, -9); camera.transform.LookAt(Vector3.up);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .15f, .19f);
        camera.targetTexture = new RenderTexture(512, 512, 24);
        float savedTimeScale = Time.timeScale;
        try
        {
            foreach (var path in new[] { "Assets/ProjectVector/Shared/Characters/Effects/Controller/Controller/ControllerSmoke.prefab", "Assets/ProjectVector/Shared/Characters/Effects/Entry/LegacySmoke/Smoke Effect.prefab" })
            {
                string name = path.Contains("ControllerSmoke") ? "Controller" : "Entry";
                var smoke = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), new Vector3(0, 1, -3), Quaternion.identity);
                smoke.transform.localScale = Vector3.one * 2;
                foreach (var vfx in smoke.GetComponentsInChildren<ControllerSmokeVfx>()) { vfx.Begin(60); vfx.Advance(1); }
                foreach (var particles in smoke.GetComponentsInChildren<ParticleSystem>()) particles.Simulate(1, false, true);
                yield return null; // Let the production render pipeline/materials initialize.
                var hidden = Render(camera, Path.Combine(output, name + "-Normal.png"));
                Check(outline.SmokeSuppressed, name + " ordinary enemy concealed");
                outline.OutlineMode = Outline.Mode.OutlineAndSilhouette; outline.OutlineColor = Color.yellow;
                Render(camera); Check(outline.SmokeSuppressed, name + " enum/color alone cannot reveal"); player.RefreshOutline();
                receiver.ReceiveReveal(Packet(1, 20, 2.5f));
                var revealed = Render(camera, Path.Combine(output, name + "-Revealed.png"));
                Check(player.HasConfirmedSkillReveal && !outline.SmokeSuppressed, name + " confirmed packet reveals enemy through smoke");
                Check(YellowPixels(revealed) > YellowPixels(hidden) + 30, name + " actual rendered yellow silhouette pixels visible");
                var fill = body.sharedMaterials.Single(m => m && m.name.StartsWith("OutlineFill"));
                Check(fill.GetFloat("_ZTest") == (float)UnityEngine.Rendering.CompareFunction.Always, name + " confirmed reveal depth mode");
                // Confirmed target can move inside or behind smoke without being rescanned locally.
                smoke.transform.position = Vector3.up; Render(camera); Check(!outline.SmokeSuppressed, name + " detected target inside smoke visible");
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = new Vector3(0, 1, -1.5f); wall.transform.localScale = new Vector3(3, 3, .2f);
                Check(YellowPixels(Render(camera)) > 30, name + " prior confirmed hit stays visible behind cover");
                Object.DestroyImmediate(wall);
                receiver.ReceiveReveal(Packet(1, 20, .05f));
                yield return null;
                Check(player.HasConfirmedSkillReveal, name + " shorter duplicate cannot shorten reveal");
                player.EndConfirmedSkillReveal(source);
                Render(camera, Path.Combine(output, name + "-Restored.png"));
                Check(outline.SmokeSuppressed && outline.OutlineMode == Outline.Mode.OutlineVisible && outline.OutlineColor == normalColor && outline.OutlineWidth == normalWidth, name + " expiry/removal restores normal concealment/style");
                player.Configure(false, true); Render(camera); Check(!outline.SmokeSuppressed, name + " ally identification unchanged"); player.Configure(false, false);
                Object.DestroyImmediate(smoke);
            }

            var blocker = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Characters/Effects/Controller/Controller/ControllerSmoke.prefab"), new Vector3(0, 1, -3), Quaternion.identity);
            blocker.transform.localScale = Vector3.one * 2;
            // Real wall-clock expiry, including while local time is paused.
            Time.timeScale = 0;
            receiver.ReceiveReveal(Packet(1, 20, .2f)); receiver.ReceiveReveal(Packet(2, 20, 1.2f));
            float until = Time.realtimeSinceStartup + .35f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Render(camera); Check(player.HasConfirmedSkillReveal && !outline.SmokeSuppressed, "one expired source cannot erase another active reveal");
            until = Time.realtimeSinceStartup + 1.05f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Render(camera); Check(!player.HasConfirmedSkillReveal && outline.SmokeSuppressed, "all sources expire while timeScale zero, smoke resumes");
            Time.timeScale = savedTimeScale;
            receiver.ReceiveReveal(Packet(1, 20, 2.5f)); receiver.ReceiveReveal(Packet(2, 20, 2.5f));
            source.ApplyDestroyFromServer(); Render(camera); Check(player.HasConfirmedSkillReveal && !outline.SmokeSuppressed, "destroying one trap retains other reveal");
            other.gameObject.SetActive(false); Render(camera); Check(!player.HasConfirmedSkillReveal && outline.SmokeSuppressed, "disabled/round-cleaned sources clear reveal"); other.gameObject.SetActive(true);
            receiver.ReceiveReveal(Packet(2, 20, 2.5f)); health.SetHp(0); Render(camera); Check(!player.HasConfirmedSkillReveal && outline.SmokeSuppressed, "death clears reveal");
            health.SetHp(100); Render(camera); Check(!player.HasConfirmedSkillReveal, "respawn does not inherit expired life reveal");
            receiver.ReceiveReveal(Packet(2, 20, 2.5f)); target.SetActive(false); target.SetActive(true); Render(camera); Check(!player.HasConfirmedSkillReveal && outline.SmokeSuppressed, "pooled target does not inherit reveal");
            receiver.ReceiveReveal(Packet(2, 20, 2.5f)); player.Configure(false, true); Check(!player.HasConfirmedSkillReveal, "team change clears reveal"); player.Configure(false, false);
            foreach (byte team in new byte[] { 1, 2, 255 }) { local.Team = team; receiver.ReceiveReveal(Packet(2, 20, 2.5f)); Check(!player.HasConfirmedSkillReveal, "non-owner team cannot receive silhouette: " + team); }
            local.Team = 0; identity.Team = 0; receiver.ReceiveReveal(Packet(2, 20, 2.5f)); Check(!player.HasConfirmedSkillReveal, "allied target cannot be revealed"); identity.Team = 1;
            player.Configure(true, true); receiver.ReceiveReveal(Packet(2, 20, 2.5f)); Check(!player.HasConfirmedSkillReveal && !outline.enabled, "local character cannot be revealed"); player.Configure(false, false);
            foreach (float duration in new[] { float.NaN, float.PositiveInfinity, -1f, 0f, 3f }) receiver.ReceiveReveal(Packet(2, 20, duration));
            receiver.ReceiveReveal(Packet(999, 20, 2)); receiver.ReceiveReveal(new byte[3]);
            Check(!player.HasConfirmedSkillReveal, "malformed/unknown-source packets rejected");
            receiver.ReceiveReveal(Packet(2, 20, 2.5f)); player.AddSmokeHide(); player.AddDreamForestHide(); Render(camera);
            Check(outline.enabled && !outline.SmokeSuppressed, "confirmed reveal wins over legacy concealment counters");
            player.EndConfirmedSkillReveal(other); Check(!outline.enabled && outline.OutlineMode == Outline.Mode.OutlineVisible, "remaining legacy concealment resumes after reveal");
            player.RemoveSmokeHide(); player.RemoveDreamForestHide(); Render(camera); Check(outline.SmokeSuppressed, "normal smoke still enforced after counter restoration");
            Debug.Log("SMOKE_SKILL_REVEAL_PASS output=" + output);
        }
        finally
        {
            Time.timeScale = savedTimeScale;
            var rt = camera.targetTexture; camera.targetTexture = null; Object.DestroyImmediate(rt);
            Object.DestroyImmediate(network.gameObject); NetworkManager.Instance = null;
        }
    }
}
#endif
