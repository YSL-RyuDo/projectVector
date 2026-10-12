using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Read-only checks; never saves scenes or rewrites gameplay ownership.
public static class VectorShopVerification
{
    [MenuItem("Tools/Project Vector/Verify Shop Assets and Protocol")]
    public static void Run()
    {
        foreach (string name in new[] { "VectorEffects", "VectorHologram" })
        {
            var shader = Resources.Load<Shader>("Shop/" + name);
            Require(shader != null, "Missing shop shader " + name);
            var material = new Material(shader);
            try { ShaderUtil.CompilePass(material, 0, true); Require(!ShaderUtil.ShaderHasError(shader), "Shader failed: " + name); }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }
        foreach (string name in new[] { "Casual1", "PicoChan", "unitychan", "Controller" })
        {
            var prefab = Resources.Load<GameObject>("LobbyCharacter/" + name);
            Require(prefab != null && prefab.GetComponentsInChildren<Renderer>(true).Length > 0, "Missing character model " + name);
        }
        foreach (string name in new[] { "InitiatorIcon", "ControllerIcon", "SentinelIcon", "DuelistIcon" })
            Require(Resources.Load<Sprite>("Sprites/" + name) != null, "Missing icon " + name);
        var database = AssetDatabase.LoadAssetAtPath<WeaponSkinDatabase>("Assets/Prefab/Weapon/Weapon Skin Database.asset");
        Require(database != null, "Missing skin database");
        int models = 0;
        foreach (var skin in database.skins.Where(s => s != null && s.skinId > 0))
        {
            Require(skin.baseWeapon != null && skin.visualModelPrefab != null, "Missing skin model " + skin.name);
            Require(Resources.LoadAll<Sprite>("Shop/ShopRewardAtlas").Any(s => s.name == $"weapon_{skin.baseWeapon.weaponId}_{skin.skinId}"), "Missing shop thumbnail " + skin.name);
            ++models;
        }
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(42UL); writer.Write((byte)0); writer.Write(1000000); writer.Write(10000);
        writer.Write((ushort)3); writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)1);
        writer.Write(0); writer.Write(1);
        for (int i = 0; i < 3; ++i)
        {
            writer.Write(100 + i); writer.Write((byte)0); writer.Write((byte)i);
            writer.Write(i); writer.Write(0); writer.Write(5000);
            var name = new byte[64]; Encoding.UTF8.GetBytes("테스트 보상 " + i).CopyTo(name, 0); writer.Write(name);
        }
        for (int i = 0; i < 2; ++i) { writer.Write(1700000000L); writer.Write(100); writer.Write((byte)0); writer.Write(0); }
        writer.Flush();
        var bytes = stream.ToArray(); var parsed = VectorShopState.Parse(bytes);
        Require(parsed.request == 42 && parsed.rewards.Count == 1 && parsed.history.Count == 1, "Packet roundtrip");
        Require(Math.Abs(parsed.items.Sum(parsed.Chance) - 100) < .00001, "Probability sum");
        // Legacy servers may advertise unlimited pages; the UI must still stop at two.
        BitConverter.GetBytes(500).CopyTo(bytes, 29);
        parsed = VectorShopState.Parse(bytes);
        Require(parsed.pages == 2 && parsed.page == 0 && parsed.history.Count == 1, "Two-page limit");
        BitConverter.GetBytes(1).CopyTo(bytes, 25);
        parsed = VectorShopState.Parse(bytes);
        Require(parsed.page == 1 && parsed.pages == 2 && parsed.history.Count == 1, "Second page retained");
        BitConverter.GetBytes(2).CopyTo(bytes, 25);
        parsed = VectorShopState.Parse(bytes);
        Require(parsed.page == 1 && parsed.history.Count == 0 && parsed.rewards.Count == 1, "Out-of-window history hidden without losing rewards");
        bool rejected = false;
        try { VectorShopState.Parse(bytes.Take(bytes.Length - 1).ToArray()); } catch (InvalidDataException) { rejected = true; }
        Require(rejected, "Truncated packet accepted");
        Debug.Log($"[SHOP VERIFY] PASS: shaders, 4 character models, {models} weapon models/icons, packet layout, probabilities, two-page history limit, malformed packet rejection.");
    }
    public static void RunBatch()
    {
        try { Run(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    private static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}
