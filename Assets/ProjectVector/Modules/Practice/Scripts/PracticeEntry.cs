using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public sealed class PracticeEntry : MonoBehaviour
{
    public Button button;
    private bool Allowed => !GamePanelController.IsMatchmakingActive &&
        (NetworkManager.Instance == null || (NetworkManager.Instance.PartyPlayers.Count < 2 && !NetworkManager.Instance.HasPendingReturnRoom));
    private void Update() { if (button != null) button.interactable = Allowed; }
    public void Enter() { if (Allowed) SceneNavigation.Load(ProjectVector.Core.ProjectScene.Practice); }
}
