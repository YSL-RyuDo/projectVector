using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

public static class RankedCaptureTestEditor
{
    public static void BuildSkillEvidence(){SkillVfxClarityAuthoring.Run();SkillVfxClarityVerification.Run();Build();}
    public static void Build()
    {
        string root = Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT");
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) throw new Exception("Missing QA output directory");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(root, "Client", "Project Vector.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Capture build failed: " + report.summary.result);
        UnityEngine.Debug.Log("RANKED_CAPTURE_BUILD_OK");
    }

    public static void RunEditor()
    {
        if (Environment.GetEnvironmentVariable("VECTOR_CAPTURE_OPT_IN") != "local-ranked-qa") throw new Exception("Explicit opt-in required");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene; refusing to replace it");
        EditorSceneManager.OpenScene("Assets/Scenes/Login.unity");
        var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
        gameView.Show(); gameView.Focus();
        EditorApplication.isPlaying = true;
    }
}
