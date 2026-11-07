#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using ProjectVector.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit local QA only. No credentials, match actions, reward claims or account writes.
public sealed class SceneTransitionSmokeTest : MonoBehaviour
{
    private int errors;
    private void OnEnable() => Application.logMessageReceived += ObserveLog;
    private void OnDisable() => Application.logMessageReceived -= ObserveLog;
    private void ObserveLog(string message, string stack, LogType kind)
    {
        if (kind == LogType.Exception || kind == LogType.Assert || kind == LogType.Error) errors++;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartIfRequested()
    {
        if (Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(), "-verify-scene-transitions") < 0) return;
        var root = new GameObject("Scene transition QA");
        DontDestroyOnLoad(root);
        var test = root.AddComponent<SceneTransitionSmokeTest>();
        test.StartCoroutine(test.Run());
    }

    private IEnumerator Run()
    {
        for (int i = 0; i < 30; i++) yield return null;
        var destinations = new[] { ProjectScene.Lobby, ProjectScene.Match, ProjectScene.Lobby,
            ProjectScene.Result, ProjectScene.Lobby, ProjectScene.RankResult,
            ProjectScene.Practice, ProjectScene.Lobby, ProjectScene.Login };
        foreach (var destination in destinations)
        {
            Debug.Log("SCENE_TRANSITION_BEGIN " + destination);
            yield return SceneNavigation.LoadAsync(destination);
            for (int i = 0; i < 90; i++) yield return null;
            var current = SceneNavigation.Current;
            // Empty ranked cache intentionally redirects to the standard result screen.
            if (current != destination && !(destination == ProjectScene.RankResult && current == ProjectScene.Result))
                Debug.LogError("SCENE_TRANSITION_WRONG_DESTINATION " + destination + " -> " + current);
            if (FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
                Debug.LogError("SCENE_TRANSITION_DUPLICATE_NETWORK " + destination);
            Debug.Log("SCENE_TRANSITION_END " + destination + " errors=" + errors);
        }
        Debug.Log(errors == 0 ? "SCENE_TRANSITION_SMOKE_PASS" : "SCENE_TRANSITION_SMOKE_FAIL errors=" + errors);
        Application.Quit(errors == 0 ? 0 : 2);
    }
}
#endif
