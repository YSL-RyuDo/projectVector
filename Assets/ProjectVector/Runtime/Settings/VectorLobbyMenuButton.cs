using UnityEngine;

// Scene buttons resolve the surviving shared instance after scene transitions.
public sealed class VectorLobbyMenuButton : MonoBehaviour
{
    public void OpenMenu() => VectorSettingsPanel.Instance?.OpenMenu();
}
