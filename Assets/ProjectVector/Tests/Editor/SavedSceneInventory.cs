using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Inventory every project-owned saved scene, including non-build legacy scenes.
public static class SavedSceneInventory
{
    [Serializable] private sealed class Entry
    {
        public string path;
        public bool inBuild;
        public int objects, missingScripts;
        public string[] modules;
    }
    [Serializable] private sealed class Report { public List<Entry> scenes = new(); }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Dedicated batch editor only");
        string output = Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT");
        if (string.IsNullOrEmpty(output) || !Directory.Exists(output)) throw new InvalidOperationException("External report folder required");
        var enabled = new HashSet<string>(EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));
        var report = new Report();
        foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/ProjectVector" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(value => value))
        {
            var scene = EditorSceneManager.OpenScene(path);
            var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            int missing = objects.Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject));
            foreach (var value in objects.Where(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject) > 0))
                Debug.LogWarning("SAVED_SCENE_MISSING_OBJECT " + path + " object=" + value.name);
            if (missing > 0)
                foreach (System.Text.RegularExpressions.Match reference in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path), @"m_Script: \{fileID: 11500000, guid: (\w+)"))
                {
                    string source = AssetDatabase.GUIDToAssetPath(reference.Groups[1].Value);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(source);
                    if (script == null || script.GetClass() == null)
                        Debug.LogWarning("SAVED_SCENE_MISSING_SOURCE " + reference.Groups[1].Value + " path=" + source);
                }
            var modules = objects.SelectMany(value => value.GetComponents<MonoBehaviour>()).Where(value => value)
                .Select(value => MonoScript.FromMonoBehaviour(value)).Where(value => value)
                .Select(AssetDatabase.GetAssetPath).Where(value => value.StartsWith("Assets/ProjectVector/Runtime/") || value.StartsWith("Assets/ProjectVector/Modules/"))
                .Select(value => value.Split('/')[3]).Distinct().OrderBy(value => value).ToArray();
            report.scenes.Add(new Entry { path = path, inBuild = enabled.Contains(path), objects = objects.Length, missingScripts = missing, modules = modules });
            Debug.Log("SAVED_SCENE_CHECK " + path + " objects=" + objects.Length + " missing=" + missing + " build=" + enabled.Contains(path));
        }
        File.WriteAllText(Path.Combine(output, "SavedSceneInventory.json"), JsonUtility.ToJson(report, true));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (report.scenes.Any(scene => scene.inBuild && scene.missingScripts != 0)) throw new Exception("Build scene has missing components");
        Debug.Log("SAVED_SCENE_INVENTORY_PASS scenes=" + report.scenes.Count + " missing=" + report.scenes.Sum(scene => scene.missingScripts));
    }
}
