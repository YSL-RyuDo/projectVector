using System;
using System.Linq;
using System.Reflection;
using ProjectVector.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLifecycleVerification
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }

    public static void Run()
    {
        Check(Application.isBatchMode, "Dedicated editor only");
        foreach (ProjectScene scene in Enum.GetValues(typeof(ProjectScene)))
        {
            if (scene == ProjectScene.Unknown) continue;
            string name = SceneCatalog.Name(scene);
            Check(SceneCatalog.Classify(name) == scene, "Scene identity " + name);
            Check(SceneCatalog.CanReconnect(name) == (scene == ProjectScene.Match || scene == ProjectScene.Result || scene == ProjectScene.RankResult), "Reconnect eligibility " + name);
            Check(!SceneCatalog.CanShowVoice(name, false, true, true, true), "Unauthenticated voice " + name);
            Check(SceneCatalog.CanShowVoice(name, true, true, false, false) == (scene == ProjectScene.Match), "Voice outside room " + name);
            Check(SceneCatalog.CanShowVoice(name, true, false, true, false) == (scene == ProjectScene.Lobby), "Voice during pick/loading " + name);
        }
        Check(SceneCatalog.Classify("SampleScene") == ProjectScene.Unknown, "Sample scene is not gameplay");
        var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
        foreach (var scene in enabled)
            Check(SceneCatalog.Classify(System.IO.Path.GetFileNameWithoutExtension(scene.path)) != ProjectScene.Unknown, "Build scene must have an explicit role: " + scene.path);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var networkObject = new GameObject("SceneBindingsFixture");
        var network = networkObject.AddComponent<NetworkManager>();
        NetworkManager.Instance = network;
        foreach (var binding in new (Type type, string field)[] {
            (typeof(LoginUI), "loginUI"), (typeof(LobbyManager), "lobbyManager"),
            (typeof(LobbyPanelController), "lobbyPanelController"), (typeof(GamePanelController), "gamePanelController"),
            (typeof(ProfilePanelController), "profilePanelController"), (typeof(ChatPanelController), "chatPanelController"),
            (typeof(FriendPanelController), "friendPanelController"), (typeof(PickManager), "pickManager"), (typeof(MatchManager), "matchManager") })
        {
            var oldOwner = new GameObject("Old " + binding.type.Name).AddComponent(binding.type);
            var newOwner = new GameObject("New " + binding.type.Name).AddComponent(binding.type);
            var set = typeof(NetworkManager).GetMethod("Set" + binding.type.Name);
            var clear = typeof(NetworkManager).GetMethod("Clear" + binding.type.Name);
            var field = typeof(NetworkManager).GetField(binding.field, Private);
            set.Invoke(network, new object[] { oldOwner });
            set.Invoke(network, new object[] { newOwner });
            clear.Invoke(network, new object[] { oldOwner });
            Check(ReferenceEquals(field.GetValue(network), newOwner), "Old scene must not detach replacement: " + binding.type.Name);
            clear.Invoke(network, new object[] { newOwner });
            Check(field.GetValue(network) == null, "Owner teardown: " + binding.type.Name);
            set.Invoke(network, new object[] { newOwner });
            typeof(NetworkManager).GetMethod("ReleaseUnloadedScene", Private).Invoke(network, new object[] { newOwner.gameObject.scene });
            Check(field.GetValue(network) == null, "Scene unload fallback: " + binding.type.Name);
            UnityEngine.Object.DestroyImmediate(oldOwner.gameObject);
            UnityEngine.Object.DestroyImmediate(newOwner.gameObject);
        }
        UnityEngine.Object.DestroyImmediate(networkObject);
        NetworkManager.Instance = null;

        var gameScene = enabled.Single(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path) == SceneCatalog.Name(ProjectScene.Match));
        var opened = EditorSceneManager.OpenScene(gameScene.path);
        var flow = UnityEngine.Object.FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
        Check(flow != null, "Saved game flow exists");
        typeof(GameController).GetField("gameSceneName", Private).SetValue(flow, "GameScene");
        typeof(GameController).GetMethod("OnSceneLoaded", Private).Invoke(flow, new object[] { opened, LoadSceneMode.Single });
        Check(flow.CurrentState == GameController.GameFlowState.Pick, "Match starts in pick even with legacy serialized scene name");
        var settings = UnityEngine.Object.FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include);
        Check(settings != null, "Saved settings exists");
        typeof(SettingsMenu).GetField("gameSceneName", Private).SetValue(settings, "GameScene");
        Check((bool)typeof(SettingsMenu).GetMethod("IsCurrentGameScene", Private).Invoke(settings, null), "Settings recognizes match with legacy serialized scene name");
        // Discard fixture field edits. Do not save or regenerate authored UI.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log("SCENE_LIFECYCLE_PASS checks=" + checks + ": scene roles, reconnect/voice policy, nine UI bindings, legacy scene names.");
    }

    public static void RunAll()
    {
        RefactorRegressionVerification.Run();
        Run();
        InteractionLifecycleVerification.RunRegression();
        SpikeRuntimeVerification.Run();
        SkillVfxClarityVerification.Run();
        Debug.Log("REFACTOR_GAMEPLAY_SUITE_PASS");
    }

    public static void RunAllSavedScenes()
    {
        RunAll();
        SavedSceneInventory.Run();
    }
}
