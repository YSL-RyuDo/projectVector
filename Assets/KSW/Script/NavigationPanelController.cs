using UnityEngine;
using UnityEngine.UI;

public enum LobbyMainMenu
{
    Lobby,
    Character,
    Game,
    Career,
    Store
}

public class NavigationPanelController : MonoBehaviour
{
    [Header("Main Navigation Content Panels")]
    [SerializeField] private GameObject lobbyHomePanel;
    [SerializeField] private GameObject characterCustomizationPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject careerPanel;
    [SerializeField] private GameObject storePanel;

    [Header("Main Navigation Buttons")]
    [SerializeField] private Button lobbyMainButton;
    [SerializeField] private Button characterMainButton;
    [SerializeField] private Button gameMainButton;
    [SerializeField] private Button careerMainButton;
    [SerializeField] private Button storeMainButton;

    [Header("Lobby Main Button Sprites")]
    [SerializeField] private Sprite lobbyNormalSprite;
    [SerializeField] private Sprite lobbySelectedSprite;

    [Header("Character Main Button Sprites")]
    [SerializeField] private Sprite characterNormalSprite;
    [SerializeField] private Sprite characterSelectedSprite;

    [Header("Career Main Button Sprites")]
    [SerializeField] private Sprite careerNormalSprite;
    [SerializeField] private Sprite careerSelectedSprite;

    [Header("Store Main Button Sprites")]
    [SerializeField] private Sprite storeNormalSprite;
    [SerializeField] private Sprite storeSelectedSprite;

    public LobbyMainMenu currentLobbyMainMenu = LobbyMainMenu.Lobby;

    private void Start()
    {
        InitializeLobbyMainNavigation();

        if (lobbyHomePanel != null)
            lobbyHomePanel.SetActive(false);

        if (characterCustomizationPanel != null)
            characterCustomizationPanel.SetActive(false);

        if (careerPanel != null)
            careerPanel.SetActive(false);

        if (storePanel != null)
            storePanel.SetActive(false);
    }

    public void SelectLobbyMainMenu(LobbyMainMenu selectedMenu)
    {
        currentLobbyMainMenu = selectedMenu;

        /*
         * 기존 Hierarchy와 기존 게임 내부 상태는 그대로 유지한다.
         * 메인 콘텐츠 루트의 활성 상태만 변경한다.
         */
        if (lobbyHomePanel != null)
            lobbyHomePanel.SetActive(selectedMenu == LobbyMainMenu.Lobby);
        if (characterCustomizationPanel != null)
            characterCustomizationPanel.SetActive(selectedMenu == LobbyMainMenu.Character);
        if (gamePanel != null)
            gamePanel.SetActive(selectedMenu == LobbyMainMenu.Game);
        if (careerPanel != null)
            careerPanel.SetActive(selectedMenu == LobbyMainMenu.Career);
        if (storePanel != null)
            storePanel.SetActive(selectedMenu == LobbyMainMenu.Store);
        ApplyLobbyMainButtonSprites();
    }

    public void InitializeLobbyMainNavigation()
    {
        BindMainNavigationButton(lobbyMainButton, OnClickLobbyMainButton);
        BindMainNavigationButton(characterMainButton, OnClickCharacterMainButton);
        BindMainNavigationButton(gameMainButton, OnClickGameButton);
        BindMainNavigationButton(careerMainButton, OnClickCareerMainButton);
        BindMainNavigationButton(storeMainButton, OnClickStoreMainButton);
    }

    private void BindMainNavigationButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            Debug.LogWarning("[LOBBY NAVIGATION] 연결되지 않은 메인 버튼이 있습니다.");
            return;
        }

        /*
         * Inspector에 남아 있던 GameObject.SetActive 이벤트와
         * 중복 호출되지 않도록 이 버튼의 클릭 이벤트만 교체한다.
         * Hierarchy나 패널 구조는 변경하지 않는다.
         */
        button.onClick = new Button.ButtonClickedEvent();

        button.onClick.AddListener(action);

        // 선택 표시는 Sprite로 직접 처리한다.
        button.transition = Selectable.Transition.None;

        if (button.image != null) button.image.color = Color.white;
    }

    public void OnClickLobbyMainButton()
    {
        SelectLobbyMainMenu(LobbyMainMenu.Lobby);
        Debug.Log("[LOBBY MAIN MENU] Lobby");
    }

    public void OnClickCharacterMainButton()
    {
        SelectLobbyMainMenu(LobbyMainMenu.Character);
        Debug.Log("[LOBBY MAIN MENU] Character");
    }

    public void OnClickCareerMainButton()
    {
        SelectLobbyMainMenu(LobbyMainMenu.Career);
        Debug.Log("[LOBBY MAIN MENU] Career");
    }

    public void OnClickStoreMainButton()
    {
        SelectLobbyMainMenu(LobbyMainMenu.Store);
        Debug.Log("[LOBBY MAIN MENU] Store");
    }

    public void OnClickGameButton()
    {
        /*
         * 게임 버튼 자체 이미지는 바꾸지 않는다.
         * 대신 로비/캐릭터/커리어/스토어 버튼은 모두 일반 이미지로 복구한다.
         */
        SelectLobbyMainMenu(LobbyMainMenu.Game);
    }

    private void ApplyLobbyMainButtonSprites()
    {
        SetMainButtonSprite(lobbyMainButton, currentLobbyMainMenu == LobbyMainMenu.Lobby ? lobbySelectedSprite : lobbyNormalSprite);
        SetMainButtonSprite(characterMainButton, currentLobbyMainMenu == LobbyMainMenu.Character ? characterSelectedSprite : characterNormalSprite);
        SetMainButtonSprite(careerMainButton, currentLobbyMainMenu == LobbyMainMenu.Career ? careerSelectedSprite : careerNormalSprite);
        SetMainButtonSprite(storeMainButton, currentLobbyMainMenu == LobbyMainMenu.Store ? storeSelectedSprite : storeNormalSprite);

        /*
         * gameMainButton은 선택 상태와 무관하게 기존 Sprite를 그대로 유지한다.
         * 따라서 이 함수에서 gameMainButton의 Sprite는 변경하지 않는다.
         */
    }

    private void SetMainButtonSprite(Button button, Sprite sprite)
    {
        if (button == null)
            return;

        Image image = button.image;

        if (image == null)
            image = button.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogError($"[MAIN BUTTON] Image가 없습니다. " + $"button={button.name}");
            return;
        }
        if (sprite == null)
        {
            Debug.LogWarning($"[MAIN BUTTON] Sprite가 없습니다. " + $"button={button.name}");
            return;
        }
        image.sprite = sprite;
        image.color = Color.white;
    }
}
