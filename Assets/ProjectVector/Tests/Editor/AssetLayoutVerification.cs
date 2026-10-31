using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Read-only import/Resources regression for the folder migration.
public static class AssetLayoutVerification
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Dedicated batch editor only");
        foreach (var scene in EditorBuildSettings.scenes)
            if (ProjectEditorPaths.Scene(Path.GetFileNameWithoutExtension(scene.path)) != scene.path)
                throw new Exception("Editor scene lookup mismatch: " + scene.path);
        int loaded = 0, aliases = 0;
        var paths = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.StartsWith("Assets/") && path.Contains("/Resources/") && !path.Contains("/Editor/"))
            .Where(path => !AssetDatabase.IsValidFolder(path)).ToArray();
        foreach (string path in paths)
        {
            if (AssetDatabase.IsValidFolder(path)) continue;
            Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == null || type == typeof(MonoScript) || type == typeof(SceneAsset) || type == typeof(DefaultAsset)) continue;
            string key = path.Substring(path.IndexOf("/Resources/", StringComparison.Ordinal) + 11);
            key = key.Substring(0, key.Length - Path.GetExtension(key).Length);
            UnityEngine.Object asset = Resources.Load(key, type);
            // Some imported characters already ship both PNG and PSD for one
            // Resources key. Preserve that catalog; do not pick/delete a copy.
            string actual = AssetDatabase.GetAssetPath(asset);
            bool sameKey = asset && paths.Contains(actual) && ResourceKey(actual) == key;
            if (!sameKey)
                throw new Exception("Resources identity mismatch: " + key + " expected=" + path + " actual=" + AssetDatabase.GetAssetPath(asset));
            if (actual != path) aliases++;
            loaded++;
        }
        Debug.Log("ASSET_RESOURCES_PASS loaded=" + loaded + " existingVendorAliases=" + aliases);
        SceneLifecycleVerification.RunAllSavedScenes();
    }

    private static string ResourceKey(string path)
    {
        string key = path.Substring(path.IndexOf("/Resources/", StringComparison.Ordinal) + 11);
        return key.Substring(0, key.Length - Path.GetExtension(key).Length);
    }
}
