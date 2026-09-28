#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Script-only builds can pair a new managed layout with stale serialized scene data.
[InitializeOnLoad]
public static class SafePlayerBuild
{
    static SafePlayerBuild() => BuildPlayerWindow.RegisterBuildPlayerHandler(BuildFromWindow);

    private static void BuildFromWindow(BuildPlayerOptions options)
    {
        // Always rebuild player data as one consistent set.  A normal Build
        // after a script/layout change must not reuse a stale scene bundle.
        options.options |= BuildOptions.CleanBuildCache;
        if ((options.options & BuildOptions.BuildScriptsOnly) != 0)
        {
            options.options &= ~BuildOptions.BuildScriptsOnly;
            Debug.LogWarning("[SAFE BUILD] Script-only build replaced with a clean full build to keep scripts and serialized scene data consistent.");
        }
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
    }

    [MenuItem("Project Vector/Build/Clean Recovery Build")]
    public static void RecoveryBuild()
    {
        string directory = Path.GetFullPath("BuildRecovery-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        if (Directory.Exists(directory)) throw new InvalidOperationException("Recovery output already exists.");
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(directory, "Project Vector.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CleanBuildCache
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Recovery build failed: " + report.summary.result);
        Debug.Log("[SAFE BUILD] RECOVERY SUCCESS: " + directory);
    }
}
#endif
