#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Read-only prefab regression. Invoke explicitly in an isolated batch editor.
public static class EntryStealthVerification
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Flags).GetValue(value);
    static void Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Flags).Invoke(value, args);
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static void RunAndBuild() { Run(); RankedCaptureTestEditor.Build(); }
    public static void Run()
    {
        Require(Application.isBatchMode, "Isolated batch editor required");
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefab/Character/Entry/Entry.prefab");
        var skill = root.GetComponent<EntrySkill_E>();
        try
        {
            Call(skill, "ResolveRoots"); Call(skill, "CacheRemoteRenderers");
            var renderers = Field<Renderer[]>(skill, "_remoteRenderers");
            var originals = Field<Material[][]>(skill, "_remoteOriginalMaterials");
            var enabled = Field<bool[]>(skill, "_remoteOriginalEnabled");
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(t => t.localPosition).ToArray();
            var rotations = transforms.Select(t => t.localRotation).ToArray();
            int checkedTextures = 0;
            Call(skill, "ApplyRemoteAlpha", 0f);
            Require(renderers.All(r => !r || !r.enabled), "Hidden enemy has a visible renderer");
            Call(skill, "ApplyRemoteAlpha", .2f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i] || !enabled[i]) continue;
                var shown = renderers[i].sharedMaterials;
                for (int j = 0; j < originals[i].Length; j++)
                {
                    var source = originals[i][j];
                    if (!source || source.shader.name.IndexOf("Toon", StringComparison.OrdinalIgnoreCase) < 0 || !source.HasProperty("_MainTex")) continue;
                    var texture = source.GetTexture("_MainTex"); if (!texture) continue;
                    Require(shown[j].GetTexture("_BaseMap") == texture, "Toon colour texture was lost: " + source.name);
                    Require(shown[j].GetTextureScale("_BaseMap") == source.GetTextureScale("_MainTex"), "Texture tiling changed");
                    Require(shown[j].GetTextureOffset("_BaseMap") == source.GetTextureOffset("_MainTex"), "Texture offset changed");
                    checkedTextures++;
                }
            }
            Require(checkedTextures > 0, "No real Entry toon materials checked");
            foreach (float alpha in new[] { 1f, 0f, 1f })
            {
                Call(skill, "ApplyRemoteAlpha", alpha);
                for (int i = 0; i < renderers.Length; i++) if (renderers[i])
                {
                    Require(renderers[i].enabled == (alpha > 0 && enabled[i]), "Renderer visibility not restored");
                    if (alpha > 0 && enabled[i]) Require(renderers[i].sharedMaterials.SequenceEqual(originals[i]), "Revealed character does not use original materials");
                }
            }
            for (int i = 0; i < transforms.Length; i++)
                Require(transforms[i].localPosition == positions[i] && Quaternion.Angle(transforms[i].localRotation, rotations[i]) < .001f, "Stealth changed a bone or weapon transform");
            var view = new SerializedObject(root.GetComponent<PlayerWeaponViewController>());
            var holder = (GameObject)view.FindProperty("thirdPersonWeaponHolder").objectReferenceValue;
            Require(holder && holder.transform.IsChildOf(root.transform.Find("Model")), "Wrong weapon holder");
            Debug.Log("ENTRY_STEALTH_PASS: hidden/revealed cycles; original materials restored; " + checkedTextures + " toon texture slots; disabled renderers and all bone/weapon transforms preserved");
            Debug.Log("ENTRY_WEAPON_MOUNT: " + AnimationUtility.CalculateTransformPath(holder.transform, root.transform) + " localPosition=" + holder.transform.localPosition + " localRotation=" + holder.transform.localEulerAngles);
        }
        finally
        {
            // These temporary copies are test-owned; never edit the source materials.
            var copies = Field<Material[][]>(skill, "_remoteRuntimeMaterials");
            if (copies != null) foreach (var group in copies) if (group != null) foreach (var m in group) if (m) Object.DestroyImmediate(m);
            typeof(EntrySkill_E).GetField("_remoteRuntimeMaterials", Flags).SetValue(skill, null);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
