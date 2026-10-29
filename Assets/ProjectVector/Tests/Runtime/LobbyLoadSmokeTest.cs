using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in standalone smoke test: no credentials, account changes or reward claims.
public sealed class LobbyLoadSmokeTest : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartIfRequested()
    {
        if (Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(), "-verify-lobby-load") < 0)
            return;
        var runner = new GameObject("LobbyLoadSmokeTest");
        DontDestroyOnLoad(runner);
        runner.AddComponent<LobbyLoadSmokeTest>().StartCoroutine(Verify());
    }

    private static IEnumerator Verify()
    {
        // Never load another scene from inside AfterSceneLoad's call stack.
        for (int i = 0; i < 10; ++i) yield return null;
        Debug.Log("[LOBBY LOAD VERIFY] Loading Lobby using the login scene transition.");
        SceneManager.LoadScene("Lobby");
        for (int i = 0; i < 120; ++i) yield return null;
        if (SceneManager.GetActiveScene().name != "Lobby")
        {
            Debug.LogError("[LOBBY LOAD VERIFY] FAIL: Lobby not active.");
            Application.Quit(2);
            yield break;
        }
        Debug.Log("[LOBBY LOAD VERIFY] PASS: Lobby deserialized and ran for 120 frames.");
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-verify-keep-open") < 0)
            Application.Quit(0);
    }
}
