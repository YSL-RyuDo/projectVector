#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch-only diagnostic entry points; no automatic work in an open editor.
public static class LobbyCrashDiagnostics
{
    public static void BuildRelease() => Build("fixed-release", false);
    public static void BuildDevelopment() => Build("fixed-development", true);
    public static void ValidateScene()
    {
        SceneScriptReferenceBuildGuard.ValidateEnabledScenes();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var components = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
        if (components.Any(c => c == null))
            throw new InvalidOperationException("Lobby contains a missing script.");
        int drag = components.OfType<MyPageStatDragItem>().Count();
        int drop = components.OfType<MyPageStatDropSlot>().Count();
        int medal = components.OfType<ChipCraftMedalDragHandle>().Count();
        if (drag != 15 || drop != 7 || medal != 3)
            throw new InvalidOperationException($"Repaired components missing: drag={drag}, drop={drop}, medal={medal}");
        Debug.Log($"[LOBBY DIAGNOSTICS] SCENE PASS: drag={drag}, drop={drop}, medal={medal}");
    }

    private static void Build(string mode, bool development)
    {
        var directory = Path.GetFullPath("BuildDiagnostics-" + mode);
        var options = BuildOptions.CleanBuildCache;
        if (development) options |= BuildOptions.Development;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(directory, "Project Vector.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = options
        });
        File.WriteAllText("Temp/codex-lobby-build.result", report.summary.result + " " + directory);
        Debug.Log("[LOBBY DIAGNOSTICS] " + report.summary.result + " " + directory);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Diagnostic build failed: " + report.summary.result);
    }
}
#endif
