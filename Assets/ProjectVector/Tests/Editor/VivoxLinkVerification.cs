#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit batch-only SDK smoke test. Never logs in, joins a channel or records audio.</summary>
[InitializeOnLoad]
public static class VivoxLinkVerification
{
    const string ActiveKey = "Vector.VivoxLinkVerification.Active";
    const string ResultKey = "Vector.VivoxLinkVerification.Result";
    const string ProjectId = "cb6df68c-44d8-4cfd-8417-93d2a927e90f";
    static double deadline;

    static VivoxLinkVerification()
    {
        EditorApplication.playModeStateChanged += OnState;
        if (SessionState.GetBool(ActiveKey, false)) WatchTimeout();
    }

    public static void Run()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run only in a dedicated batch Editor with the interactive project closed.");
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            if (EditorSceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Refusing to replace an unsaved scene.");
        // Use the same saved project identity as the runtime Core package. The
        // Editor account service can be empty in a headless batch session.
        if (Application.cloudProjectId != ProjectId)
            throw new InvalidOperationException("Unexpected Cloud project; refusing to initialize.");

        var settingsType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("Unity.Services.Vivox.Editor.VivoxSettings"))
            .FirstOrDefault(t => t != null);
        if (settingsType == null) throw new InvalidOperationException("Vivox Editor settings unavailable.");
        var settings = settingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        foreach (var name in new[] { "Server", "Domain", "TokenIssuer" })
            if (string.IsNullOrWhiteSpace((string)settingsType.GetProperty(name).GetValue(settings)))
                throw new InvalidOperationException("Vivox connection settings missing: " + name);
        if ((bool)settingsType.GetProperty("IsTestMode").GetValue(settings)
            || !string.IsNullOrEmpty((string)settingsType.GetProperty("TokenKey").GetValue(settings)))
            throw new InvalidOperationException("Disable client token test mode and remove the local signing key before this test.");

        Debug.Log("VIVOX_LINK: Cloud project and local non-secret connection settings verified; test mode off.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetInt(ResultKey, 1);
        SessionState.SetBool(ActiveKey, true);
        WatchTimeout();
        EditorApplication.EnterPlaymode();
    }

    static void WatchTimeout()
    {
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false) || EditorApplication.timeSinceStartup < deadline) return;
        Debug.LogError("VIVOX_LINK: Initialization timed out; no login or channel join was requested.");
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.Exit(1);
    }

    static void OnState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) _ = VerifyAsync();
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.Exit(SessionState.GetInt(ResultKey, 1));
        }
    }

    static async Task VerifyAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await VivoxService.Instance.InitializeAsync();
            if (VivoxService.Instance.InitializationState != VivoxInitializationState.Initialized)
                throw new InvalidOperationException("SDK did not reach Initialized state.");
            VivoxService.Instance.Uninitialize();
            SessionState.SetInt(ResultKey, 0);
            Debug.Log("VIVOX_LINK: PASS - UGS and Vivox initialized and shut down. No authentication, login, channel join or voice transmission tested.");
        }
        catch (Exception error)
        {
            // SDK messages may contain endpoints or tokens; log only the exception type.
            Debug.LogError("VIVOX_LINK: FAIL - " + error.GetType().Name);
            if (VivoxService.Instance != null && VivoxService.Instance.InitializationState == VivoxInitializationState.Initialized)
                VivoxService.Instance.Uninitialize();
        }
        finally { EditorApplication.ExitPlaymode(); }
    }
}
#endif
