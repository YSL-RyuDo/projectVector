using UnityEngine;
using UnityEngine.SceneManagement;

// Scene objects register while alive; account, transport and match state survive scene changes.
public partial class NetworkManager
{
    public void SetLoginUI(LoginUI ui)
    {
        loginUI = ui;
    }

    public void SetLobbyManager(LobbyManager manager)
    {
        lobbyManager = manager;
    }

    public void SetLobbyPanelController(LobbyPanelController controller)
    {
        lobbyPanelController = controller;
    }

    public void ClearLobbyPanelController(LobbyPanelController controller)
    {
        if (ReferenceEquals(lobbyPanelController, controller))
            lobbyPanelController = null;
    }

    public void SetGamePanelController(GamePanelController controller)
    {
        gamePanelController = controller;
    }

    public void SetProfilePanelController(ProfilePanelController controller)
    {
        profilePanelController = controller;
    }

    public void SetChatPanelController(ChatPanelController controller)
    {
        chatPanelController = controller;
        SyncLobbyRoomChatContext();
    }

    public void ClearChatPanelController(ChatPanelController controller)
    {
        if (ReferenceEquals(chatPanelController, controller))
            chatPanelController = null;
    }

    public void SetFriendPanelController(FriendPanelController controller)
    {
        friendPanelController = controller;
    }

    public void ClearFriendPanelController(FriendPanelController controller)
    {
        if (ReferenceEquals(friendPanelController, controller))
            friendPanelController = null;
    }

    public void SetPickManager(PickManager manager)
    {
        pickManager = manager;
    }

    public void SetMatchManager(MatchManager manager)
    {
        matchManager = manager;
    }

    public void ClearLoginUI(LoginUI owner)
    {
        if (ReferenceEquals(loginUI, owner)) loginUI = null;
    }

    public void ClearLobbyManager(LobbyManager owner)
    {
        if (ReferenceEquals(lobbyManager, owner)) lobbyManager = null;
    }

    public void ClearGamePanelController(GamePanelController owner)
    {
        if (ReferenceEquals(gamePanelController, owner)) gamePanelController = null;
    }

    public void ClearProfilePanelController(ProfilePanelController owner)
    {
        if (ReferenceEquals(profilePanelController, owner)) profilePanelController = null;
    }

    public void ClearPickManager(PickManager owner)
    {
        if (ReferenceEquals(pickManager, owner)) pickManager = null;
    }

    public void ClearMatchManager(MatchManager owner)
    {
        if (ReferenceEquals(matchManager, owner)) matchManager = null;
    }

    private void OnEnable() => SceneManager.sceneUnloaded += ReleaseUnloadedScene;
    private void OnDisable() => SceneManager.sceneUnloaded -= ReleaseUnloadedScene;

    private static void ReleaseSceneReference<T>(ref T value, Scene scene) where T : Component
    {
        if (!value || value.gameObject.scene == scene) value = null;
    }

    private void ReleaseUnloadedScene(Scene scene)
    {
        ReleaseSceneReference(ref loginUI, scene);
        ReleaseSceneReference(ref lobbyManager, scene);
        ReleaseSceneReference(ref lobbyPanelController, scene);
        ReleaseSceneReference(ref gamePanelController, scene);
        ReleaseSceneReference(ref profilePanelController, scene);
        ReleaseSceneReference(ref chatPanelController, scene);
        ReleaseSceneReference(ref friendPanelController, scene);
        ReleaseSceneReference(ref pickManager, scene);
        ReleaseSceneReference(ref matchManager, scene);
    }
}
