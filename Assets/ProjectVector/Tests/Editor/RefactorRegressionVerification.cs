using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ProjectVector.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RefactorRegressionVerification
{
    private static int checks;
    private static void Check(bool value, string reason)
    {
        if (!value) throw new Exception(reason);
        checks++;
    }

    public static void Run()
    {
        Check(Application.isBatchMode, "Run on saved assets in a dedicated batch editor.");
        VisibilityOwnership();
        Purchases();
        InventoryVisibility();
        SceneScriptReferenceBuildGuard.ValidateEnabledScenes();
        foreach (var scene in EditorBuildSettings.scenes.Where(value => value.enabled))
        {
            var opened = EditorSceneManager.OpenScene(scene.path);
            int missing = opened.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject));
            Check(missing == 0, scene.path + ": missing script components: " + missing);
        }
        Debug.Log("REFACTOR_REGRESSION_PASS checks=" + checks + ": independent visibility owners, purchase protocol/state, saved scene script references.");
    }

    private static void VisibilityOwnership()
    {
        var state = new EquipmentVisibilityState();
        object skill = new object(), camera = new object(), death = new object();
        Check(state.IsVisible, "initial visibility");
        state.SetVisibleFor(skill, false);
        state.SetVisibleFor(skill, false);
        state.SetVisibleFor(camera, true);
        Check(!state.IsVisible, "camera refresh must not override a skill");
        state.SetVisibleFor(death, false);
        state.SetVisibleFor(skill, true);
        Check(!state.IsVisible, "skill cancellation must not override death");
        state.SetVisibleFor(death, true);
        Check(state.IsVisible, "repeated hides do not accumulate reference counts");
        state.SetVisibleFor(skill, false);
        state.ResetForNewLife();
        Check(state.IsVisible, "respawn clears stale owners");
    }

    private static byte[] Requests(uint room, byte team, params (uint player, ulong token)[] members)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(room); writer.Write(team); writer.Write((byte)0); writer.Write((byte)members.Length);
        foreach (var entry in members)
        {
            writer.Write(entry.player); writer.Write(entry.token); writer.Write(3); writer.Write(800);
            writer.Write((uint)0); writer.Write((byte)1);
        }
        return stream.ToArray();
    }

    private static void Purchases()
    {
        var team = new HashSet<uint> { 11, 12, 13 };
        var packet = Requests(5, 0, (11, 100), (12, 101));
        Check(TeamPurchaseState.TryRead(packet, 5, 0, team, out var entries), "valid team requests");
        var state = new TeamPurchaseState(); state.Replace(5, entries);
        Check(state.ForRoom(5).Count == 2 && state.ForRoom(6) == null, "room scoped requests");
        Check(!TeamPurchaseState.TryRead(packet, 6, 0, team, out _), "stale room rejected");
        Check(!TeamPurchaseState.TryRead(packet, 5, 1, team, out _), "enemy team rejected");
        Check(!TeamPurchaseState.TryRead(Requests(5, 0, (11, 100), (11, 101)), 5, 0, team, out _), "duplicate player rejected");
        Check(!TeamPurchaseState.TryRead(Requests(5, 0, (11, 100), (12, 100)), 5, 0, team, out _), "duplicate token rejected");
        Check(!TeamPurchaseState.TryRead(Requests(5, 0, (99, 100)), 5, 0, team, out _), "unknown player rejected");
        for (int length = 0; length < packet.Length; length++)
            Check(!TeamPurchaseState.TryRead(packet.Take(length).ToArray(), 5, 0, team, out _), "truncated request " + length);
        int callbacks = 0, credits = -1;
        Action<int> receive = amount => { callbacks++; credits = amount; };
        state.CreditsReceived += receive;
        Check(state.ApplyResult(new byte[] { 0, 200, 0, 0, 0 }) && credits == 200 && callbacks == 1, "confirmed result event");
        Check(!state.ApplyResult(new byte[] { 0, 255, 255, 255, 255 }) && callbacks == 1, "negative budget rejected");
        state.CreditsReceived -= receive;
        state.ApplyResult(new byte[] { 3, 100, 0, 0, 0 });
        Check(callbacks == 1 && state.ResultCode == 3, "detached UI is not called");
        state.Reset(); Check(state.ResultCode == 0 && state.ForRoom(5) == null, "round/room reset");
    }

    private static void InventoryVisibility()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var actor = new GameObject("Equipment owner fixture");
        var inventory = actor.AddComponent<PlayerItem>();
        var weaponObject = new GameObject("Held weapon"); weaponObject.transform.SetParent(actor.transform);
        inventory.Items[0] = weaponObject.AddComponent<Weapon>();
        typeof(PlayerItem).GetField("currentIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(inventory, 0);
        object skill = new object(), camera = new object();
        inventory.SetWeaponVisibility(false, skill);
        inventory.SetWeaponVisibility(true, camera);
        Check(!weaponObject.activeSelf, "inventory respects other owners");
        inventory.SetWeaponVisibility(true, skill);
        Check(weaponObject.activeSelf, "last owner released");
        inventory.SetWeaponVisibility(false, skill);
        inventory.EnableOnRespawn();
        Check(weaponObject.activeSelf, "new life restores selected slot");
        UnityEngine.Object.DestroyImmediate(actor);
    }
}
