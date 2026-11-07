using ProjectVector.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

// Navigation only: server acknowledgement, rewards and reconnect snapshots remain
// owned by their feature. In particular, reconnect may reload the current scene.
public static class SceneNavigation
{
    public static ProjectScene Current => SceneCatalog.Classify(SceneManager.GetActiveScene().name);
    public static void Load(ProjectScene destination) => SceneManager.LoadScene(SceneCatalog.Name(destination));
    public static AsyncOperation LoadAsync(ProjectScene destination) => SceneManager.LoadSceneAsync(SceneCatalog.Name(destination));
}
