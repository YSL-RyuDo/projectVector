using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct LobbyBackgroundPreset
{
    public Vector3 characterPosition;
    public Vector3 characterRotation;
    public Vector3 cameraPosition;
    public Vector3 cameraRotation;
}

public class LobbyManager : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image playerIcon;
    [SerializeField] private Sprite[] Icon;
    [SerializeField] private Slider expSlider;

    private int currentExp;
    private int requiredExp;

    [Header("Character")]
    [SerializeField] private GameObject[] CharacterPrefebs;
    [SerializeField] private Camera lobbyCamera;
    private GameObject currentCharacter;

    [Header("Background")]
    [SerializeField] private LobbyBackgroundPreset[] backgroundPresets;

    [Header("Character Popup")]
    [SerializeField] private GameObject characterSelectPopup;
    [SerializeField] private Transform characterSlotContent;
    [SerializeField] private GameObject characterSlotPrefab;

    [Header("Background Popup")]
    [SerializeField] private GameObject backgroundSelectPopup;

    [Header("Chat")]
    [SerializeField] private GameObject ChatPrefeb;
    [SerializeField] private Transform ChatSlotContent;
    [SerializeField] private TMP_InputField ChatInputField;

    [Header("Game UI Root")]
    [SerializeField] private GameObject gamePanel;

    [Header("Temp Game Room")]
    [SerializeField] private GameObject tempGameRoomPanel;
    [SerializeField] private TempGamePlayerSlotUI[] tempGamePlayerSlots;
    [SerializeField] private Sprite[] tempRoomIconSprites;
    [SerializeField] private Sprite[] tempRoomBorderSprites;
    [SerializeField] private Sprite[] tempRoomRankSprites;

    [Header("Room List")]
    [SerializeField] private GameObject roomListPanel;
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomSlotPrefab;

    [Header("Room Panel")]
    [SerializeField] private RoomPanelUI roomPanelUI;

    [Header("Game Mode Buttons")]
    [SerializeField] private Button normalGameButton;
    [SerializeField] private Button rankGameButton;
    [SerializeField] private Button customRoomButton;

    [SerializeField] private Color unselectedModeButtonColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color selectedModeButtonColor = new Color(0.75f, 1f, 0.35f, 1f);

    private readonly List<GameObject> characterSlots = new();
    private readonly List<GameObject> roomSlotObjects = new();

    private enum GamePanelView
    {
        None,
        RoomList,
        TempGameRoom,
        RoomPanel
    }

    private enum GameMode
    {
        None,
        Normal,
        Rank,
        Custom
    }

    private enum AfterRoomLeaveAction
    {
        OpenRoomList,
        EnterNormal,
        EnterRank
    }

    private GamePanelView currentGamePanelView = GamePanelView.None;
    private GameMode currentGameMode = GameMode.Normal;
    private GameMode previousModeBeforeCustom = GameMode.Normal;
    private AfterRoomLeaveAction afterRoomLeaveAction = AfterRoomLeaveAction.OpenRoomList;

    private RoomStateData currentRoomState;

    private string currentNickname;
    private int currentLevel;
    private int currentplayerIconId;
    private int currentlobbyCharacterId;
    private int currentlobbyBackgroundId;

    public static LobbyManager Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        NetworkManager.Instance.SetLobbyManager(this);
        NetworkManager.Instance.SendLobbyEnter();

        if (characterSelectPopup != null)
            characterSelectPopup.SetActive(false);

        if (backgroundSelectPopup != null)
            backgroundSelectPopup.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(false);

        if (roomListPanel != null)
            roomListPanel.SetActive(false);

        if (roomPanelUI != null)
        {
            roomPanelUI.HideRoom();
            roomPanelUI.OnConfirmLeave = OnConfirmRoomLeave;
        }

        HideAllTempGameSlots();

        SelectGameMode(GameMode.Normal);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (roomPanelUI != null && roomPanelUI.IsRoomOpen())
        {
            if (roomPanelUI.IsLeaveConfirmOpen())
            {
                roomPanelUI.HideLeaveConfirmPanel();
            }
            else
            {
                afterRoomLeaveAction = AfterRoomLeaveAction.OpenRoomList;
                roomPanelUI.ShowLeaveConfirmPanel();
            }

            return;
        }

        if (backgroundSelectPopup != null)
            backgroundSelectPopup.SetActive(false);

        if (characterSelectPopup != null)
            characterSelectPopup.SetActive(false);
    }

    public void OnRecvLobbyEnter(
        string nickname,
        int level,
        int currentExpValue,
        int requiredExpValue,
        int playerIconId,
        int lobbyCharacterModelId,
        int lobbyBackgroundId)
    {
        currentNickname = nickname;
        currentLevel = level;
        currentExp = currentExpValue;
        requiredExp = requiredExpValue;

        currentplayerIconId = playerIconId;
        currentlobbyCharacterId = lobbyCharacterModelId;
        currentlobbyBackgroundId = lobbyBackgroundId;

        if (nicknameText != null)
            nicknameText.text = nickname;

        if (levelText != null)
            levelText.text = "Lv." + level;

        if (Icon != null && Icon.Length > 0 && playerIcon != null)
        {
            if (currentplayerIconId < 0 || currentplayerIconId >= Icon.Length)
                currentplayerIconId = 0;

            playerIcon.sprite = Icon[currentplayerIconId];
        }

        ApplyExpUI(currentExp, requiredExp);
        ApplyCharacter(currentlobbyCharacterId);
        ApplyBackgroundPreset(currentlobbyBackgroundId);
    }

    private void ApplyCharacter(int characterId)
    {
        if (currentCharacter != null)
            Destroy(currentCharacter);

        if (CharacterPrefebs == null || CharacterPrefebs.Length == 0)
            return;

        if (characterId < 0 || characterId >= CharacterPrefebs.Length)
            characterId = 0;

        currentCharacter = Instantiate(CharacterPrefebs[characterId]);
    }

    private void ApplyBackgroundPreset(int backgroundId)
    {
        if (backgroundPresets == null || backgroundPresets.Length == 0)
            return;

        if (backgroundId < 0 || backgroundId >= backgroundPresets.Length)
            backgroundId = 0;

        LobbyBackgroundPreset preset = backgroundPresets[backgroundId];

        if (currentCharacter != null)
        {
            currentCharacter.transform.position = preset.characterPosition;
            currentCharacter.transform.rotation = Quaternion.Euler(preset.characterRotation);
        }

        if (lobbyCamera != null)
        {
            lobbyCamera.transform.position = preset.cameraPosition;
            lobbyCamera.transform.rotation = Quaternion.Euler(preset.cameraRotation);
        }
    }

    public void OnRecvLobbyCharacterSelectResult(byte result, int modelId)
    {
        if (result != 1)
            return;

        currentlobbyCharacterId = modelId;

        ApplyCharacter(currentlobbyCharacterId);
        ApplyBackgroundPreset(currentlobbyBackgroundId);

        if (characterSelectPopup != null)
            characterSelectPopup.SetActive(false);
    }

    public void OnRecvLobbyBackgroundSelectResult(byte result, int backgroundId)
    {
        if (result != 1)
            return;

        currentlobbyBackgroundId = backgroundId;
        ApplyBackgroundPreset(currentlobbyBackgroundId);

        if (backgroundSelectPopup != null)
            backgroundSelectPopup.SetActive(false);
    }

    public void OnRecvLobbyCharacterList(List<LobbyCharacterData> list, int currentCharacterModelId)
    {
        currentlobbyCharacterId = currentCharacterModelId;

        for (int i = 0; i < characterSlots.Count; i++)
            Destroy(characterSlots[i]);

        characterSlots.Clear();

        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].owned)
                continue;

            int characterId = list[i].characterId;

            GameObject slot = Instantiate(characterSlotPrefab, characterSlotContent);
            characterSlots.Add(slot);

            TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);
            for (int j = 0; j < texts.Length; j++)
            {
                if (j == 0)
                    texts[j].text = "Character " + characterId;
                else if (j == 1)
                    texts[j].text = characterId == currentlobbyCharacterId ? "¼±ÅÃÁß" : "";
            }

            Button button = slot.GetComponent<Button>();
            if (button == null)
                button = slot.GetComponentInChildren<Button>(true);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClickCharacterSelect(characterId));
            }
        }

        if (characterSelectPopup != null)
            characterSelectPopup.SetActive(true);
    }

    public void OnClickOpenCharacterSelect()
    {
        NetworkManager.Instance.SendLobbyCharacterList();
    }

    public void OnClickCharacterSelect(int characterId)
    {
        NetworkManager.Instance.SendLobbyCharacterSelect(characterId);
    }

    public void OnClickOpenBackgroundSelect()
    {
        if (backgroundSelectPopup != null)
            backgroundSelectPopup.SetActive(true);
    }

    public void OnClickBackgroundSelect(int backgroundId)
    {
        NetworkManager.Instance.SendLobbyBackgroundSelect(backgroundId);
    }

    public void OnRecvLobbyChat(uint playerId, string nickname, string message)
    {
        if (ChatPrefeb == null || ChatSlotContent == null)
            return;

        GameObject item = Instantiate(ChatPrefeb, ChatSlotContent);
        TMP_Text text = item.GetComponentInChildren<TMP_Text>();

        if (text != null)
            text.text = nickname + " : " + message;
    }

    public void OnClickLobbyChat()
    {
        string msg = ChatInputField.text.Trim();

        if (msg == "")
            return;

        NetworkManager.Instance.SendLobbyChat(msg);
        ChatInputField.text = "";
    }

    private void ApplyExpUI(int current, int required)
    {
        if (required <= 0)
            required = 1;

        if (current < 0)
            current = 0;

        if (current > required)
            current = required;

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = required;
            expSlider.value = current;
        }
    }

    private void HideAllTempGameSlots()
    {
        if (tempGamePlayerSlots == null)
            return;

        for (int i = 0; i < tempGamePlayerSlots.Length; i++)
        {
            if (tempGamePlayerSlots[i] != null)
                tempGamePlayerSlots[i].Hide();
        }
    }

    private void ShowGamePanel()
    {
        if (gamePanel != null)
            gamePanel.SetActive(true);
    }

    private void HideGamePanel()
    {
        if (gamePanel != null)
            gamePanel.SetActive(false);
    }

    private void HideAllGameSubPanels()
    {
        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(false);

        if (roomListPanel != null)
            roomListPanel.SetActive(false);

        if (roomPanelUI != null)
            roomPanelUI.HideRoom();
    }

    private void ShowRoomListPanel()
    {
        ShowGamePanel();

        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(false);

        if (roomPanelUI != null)
            roomPanelUI.HideRoom();

        HideAllTempGameSlots();

        if (roomListPanel != null)
            roomListPanel.SetActive(true);

        currentGamePanelView = GamePanelView.RoomList;
    }

    private void ShowTempGameRoomPanel()
    {
        ShowGamePanel();

        if (roomListPanel != null)
            roomListPanel.SetActive(false);

        if (roomPanelUI != null)
            roomPanelUI.HideRoom();

        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(true);

        currentGamePanelView = GamePanelView.TempGameRoom;
    }

    private void ShowTempGameRoomPanelOnly()
    {
        ShowGamePanel();

        if (roomListPanel != null)
            roomListPanel.SetActive(false);

        if (roomPanelUI != null)
            roomPanelUI.HideRoom();

        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(true);

        currentGamePanelView = GamePanelView.TempGameRoom;
    }

    private void ShowRoomPanelOnly()
    {
        ShowGamePanel();

        if (roomListPanel != null)
            roomListPanel.SetActive(false);

        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(false);

        if (roomPanelUI != null && currentRoomState != null)
            roomPanelUI.OpenRoom(currentRoomState);

        currentGamePanelView = GamePanelView.RoomPanel;
    }

    private void FillLocalTempGameSlot0()
    {
        if (tempGamePlayerSlots == null || tempGamePlayerSlots.Length == 0)
            return;

        TempGamePlayerSlotUI slot0 = tempGamePlayerSlots[0];
        if (slot0 == null)
            return;

        Sprite icon = GetSafeSprite(tempRoomIconSprites, currentplayerIconId);
        Sprite border = GetSafeSprite(tempRoomBorderSprites, 0);
        Sprite rank = GetSafeSprite(tempRoomRankSprites, 0);

        slot0.SetData(icon, border, rank, currentNickname);

        for (int i = 1; i < tempGamePlayerSlots.Length; i++)
        {
            if (tempGamePlayerSlots[i] != null)
                tempGamePlayerSlots[i].Hide();
        }
    }

    private void EnterTempGameRoom()
    {
        ShowTempGameRoomPanel();
        HideAllTempGameSlots();
        FillLocalTempGameSlot0();
        NetworkManager.Instance.SendTempGameRoomEnter();
    }

    public void OnClickGameButton()
    {
        Debug.Log($"[GAME PANEL] GameButton Click currentView={currentGamePanelView}");

        if (currentGamePanelView == GamePanelView.None)
        {
            SelectGameMode(GameMode.Normal);
            EnterTempGameRoom();
            return;
        }

        if (currentGamePanelView == GamePanelView.TempGameRoom)
        {
            ShowTempGameRoomPanelOnly();
            return;
        }

        if (currentGamePanelView == GamePanelView.RoomPanel)
        {
            ShowRoomPanelOnly();
            return;
        }

        if (currentGamePanelView == GamePanelView.RoomList)
        {
            ShowRoomListPanel();
            return;
        }
    }

    public void OnClickNormalGameButton()
    {
        Debug.Log("[GAME MODE] NormalButton Click");

        if (currentGameMode == GameMode.Normal)
        {
            Debug.Log("[GAME MODE] already normal");
            return;
        }

        if (IsInCustomRoom())
        {
            RequestRoomLeave(AfterRoomLeaveAction.EnterNormal);
            return;
        }

        SelectGameMode(GameMode.Normal);
        EnterTempGameRoom();
    }

    public void OnClickRankGameButton()
    {
        Debug.Log("[GAME MODE] RankButton Click");

        if (currentGameMode == GameMode.Rank)
        {
            Debug.Log("[GAME MODE] already rank");
            return;
        }

        if (IsInCustomRoom())
        {
            RequestRoomLeave(AfterRoomLeaveAction.EnterRank);
            return;
        }

        SelectGameMode(GameMode.Rank);
        EnterTempGameRoom();
    }

    public void OnClickUserRoomButton()
    {
        Debug.Log("[ROOM LIST] UserRoomButton Click");

        if (currentGameMode == GameMode.Custom)
        {
            Debug.Log("[GAME MODE] already custom");
            return;
        }

        previousModeBeforeCustom = currentGameMode;

        SelectGameMode(GameMode.Custom);

        ShowRoomListPanel();
        NetworkManager.Instance.SendRoomList();
    }

    public void OnClickTempGameStart()
    {
        Debug.Log("[TEMP GAME ROOM] StartButton Click - not used in prototype");
    }

    public void OnClickCloseTempGameRoom()
    {
        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(false);

        HideAllTempGameSlots();

        currentGamePanelView = GamePanelView.None;
        SelectGameMode(GameMode.Normal);
    }

    public void OnRecvTempGameRoomEnter(int playerCount, List<TempGamePlayerData> players)
    {
        Debug.Log($"[TEMP GAME ROOM] enter success, playerCount={playerCount}");

        ShowTempGameRoomPanel();
        HideAllTempGameSlots();

        for (int i = 0; i < players.Count; i++)
        {
            TempGamePlayerData p = players[i];

            if (tempGamePlayerSlots == null)
                break;

            if (p.slotIndex < 0 || p.slotIndex >= tempGamePlayerSlots.Length)
                continue;

            if (p.slotIndex >= playerCount)
                continue;

            Sprite icon = GetSafeSprite(tempRoomIconSprites, p.playerIconId);
            Sprite border = GetSafeSprite(tempRoomBorderSprites, p.logoBorderId);
            Sprite rank = GetSafeSprite(tempRoomRankSprites, p.rankImageId);

            if (tempGamePlayerSlots[p.slotIndex] != null)
                tempGamePlayerSlots[p.slotIndex].SetData(icon, border, rank, p.nickname);
        }
    }

    public void OnRecvTempGameStartResult(byte result)
    {
        Debug.Log($"[TEMP GAME] start result={result}");
    }

    private Sprite GetSafeSprite(Sprite[] sprites, int index)
    {
        if (sprites == null || sprites.Length == 0)
            return null;

        if (index < 0 || index >= sprites.Length)
            return sprites[0];

        return sprites[index];
    }

    public void OnRecvRoomList(List<RoomListData> rooms)
    {
        Debug.Log($"[ROOM LIST UI] count={rooms.Count}");

        ShowRoomListPanel();

        for (int i = 0; i < roomSlotObjects.Count; i++)
            Destroy(roomSlotObjects[i]);

        roomSlotObjects.Clear();

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomListData room = rooms[i];

            Debug.Log(
                $"[ROOM RAW] " +
                $"index={i} " +
                $"roomId={room.roomId} " +
                $"roomName={room.roomName} " +
                $"mapName={room.mapName} " +
                $"hostName={room.hostName} " +
                $"players={room.currentPlayerCount}/{room.maxPlayerCount} " +
                $"state={room.state}"
            );

            GameObject slotObj = Instantiate(roomSlotPrefab, roomListContent);
            roomSlotObjects.Add(slotObj);

            RoomSlotUI slotUI = slotObj.GetComponent<RoomSlotUI>();
            if (slotUI != null)
                slotUI.SetData(room);
        }
    }

    public void OnClickCloseRoomListPanel()
    {
        if (roomListPanel != null)
            roomListPanel.SetActive(false);

        if (currentGamePanelView == GamePanelView.RoomList)
            currentGamePanelView = GamePanelView.None;

        if (currentRoomState == null)
        {
            if (previousModeBeforeCustom == GameMode.None || previousModeBeforeCustom == GameMode.Custom)
                previousModeBeforeCustom = GameMode.Normal;

            SelectGameMode(previousModeBeforeCustom);
        }
    }

    public void OnClickCloseGamePanel()
    {
        HideAllGameSubPanels();
        HideGamePanel();
    }

    public void OnRecvRoomCreateResult(RoomStateData roomState)
    {
        Debug.Log(
            $"[ROOM CREATE UI] " +
            $"roomId={roomState.roomId} " +
            $"roomName={roomState.roomName} " +
            $"mapName={roomState.mapName} " +
            $"isLeader={roomState.isLeader} " +
            $"canStart={roomState.canStart} " +
            $"memberCount={roomState.members.Count}"
        );

        currentRoomState = roomState;
        currentGamePanelView = GamePanelView.RoomPanel;

        SelectGameMode(GameMode.Custom);

        ShowGamePanel();

        if (roomPanelUI != null)
            roomPanelUI.OpenRoom(roomState);
        else
            Debug.LogWarning("[ROOM CREATE UI] roomPanelUI is null");
    }

    private void OnConfirmRoomLeave()
    {
        Debug.Log($"[ROOM LEAVE] confirm afterAction={afterRoomLeaveAction}");
        NetworkManager.Instance.SendRoomLeave();
    }

    public void OnRecvRoomLeaveResult(byte result, uint roomId, bool roomDeleted)
    {
        Debug.Log($"[ROOM LEAVE UI] result={result}, roomId={roomId}, roomDeleted={roomDeleted}, afterAction={afterRoomLeaveAction}");

        if (result != 0)
        {
            Debug.LogWarning($"[ROOM LEAVE UI] fail result={result}");
            return;
        }

        currentRoomState = null;

        if (roomPanelUI != null)
            roomPanelUI.HideRoom();

        AfterRoomLeaveAction action = afterRoomLeaveAction;
        afterRoomLeaveAction = AfterRoomLeaveAction.OpenRoomList;

        if (action == AfterRoomLeaveAction.EnterNormal)
        {
            SelectGameMode(GameMode.Normal);
            EnterTempGameRoom();
            return;
        }

        if (action == AfterRoomLeaveAction.EnterRank)
        {
            SelectGameMode(GameMode.Rank);
            EnterTempGameRoom();
            return;
        }

        currentGamePanelView = GamePanelView.RoomList;
        SelectGameMode(GameMode.Custom);

        ShowRoomListPanel();
        NetworkManager.Instance.SendRoomList();
    }

    private bool IsInCustomRoom()
    {
        return currentGamePanelView == GamePanelView.RoomPanel && currentRoomState != null;
    }

    private void RequestRoomLeave(AfterRoomLeaveAction action)
    {
        afterRoomLeaveAction = action;

        Debug.Log($"[ROOM LEAVE REQUEST UI] afterAction={afterRoomLeaveAction}");

        if (roomPanelUI == null || currentRoomState == null)
        {
            Debug.LogWarning("[ROOM LEAVE REQUEST UI] roomPanelUI or currentRoomState is null");
            return;
        }

        ShowGamePanel();
        roomPanelUI.OpenRoom(currentRoomState);
        roomPanelUI.ShowLeaveConfirmPanel();
    }

    private void SelectGameMode(GameMode mode)
    {
        currentGameMode = mode;
        ApplyGameModeButtonState();

        Debug.Log($"[GAME MODE SELECT] mode={currentGameMode}");
    }

    private void ApplyGameModeButtonState()
    {
        SetModeButtonState(normalGameButton, currentGameMode == GameMode.Normal);
        SetModeButtonState(rankGameButton, currentGameMode == GameMode.Rank);
        SetModeButtonState(customRoomButton, currentGameMode == GameMode.Custom);
    }

    private void SetModeButtonState(Button button, bool selected)
    {
        if (button == null)
            return;

        Color targetColor = selected ? selectedModeButtonColor : unselectedModeButtonColor;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = targetColor;

        ColorBlock colors = button.colors;
        colors.normalColor = targetColor;
        colors.highlightedColor = targetColor;
        colors.pressedColor = targetColor;
        colors.selectedColor = targetColor;
        colors.disabledColor = targetColor;
        button.colors = colors;

        button.interactable = !selected;
    }
}