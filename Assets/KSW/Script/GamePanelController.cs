using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GamePanelController : MonoBehaviour
{
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
    [SerializeField] private Button roomRefreshButton;

    [Header("Room Panel")]
    [SerializeField] private RoomPanelUI roomPanelUI;

    [Header("Game Mode Buttons")]
    [SerializeField] private Button normalGameButton;
    [SerializeField] private Button rankGameButton;
    [SerializeField] private Button customRoomButton;

    [Header("Room Slot Admin Menu")]
    [SerializeField] private GameObject slotAdminMenuPanel;
    [SerializeField] private TMP_Text slotAdminTargetText;
    [SerializeField] private Button transferLeaderButton;
    [SerializeField] private Button kickButton;

    [Header("Room Slot Swap Popup")]
    [SerializeField] private GameObject slotSwapRequestPanel;
    [SerializeField] private TMP_Text slotSwapRequestText;
    [SerializeField] private Button slotSwapAcceptButton;
    [SerializeField] private Button slotSwapRejectButton;

    [Header("Room Message")]
    [SerializeField] private GameObject roomMessagePanel;
    [SerializeField] private TMP_Text roomMessageText;
    [SerializeField] private float roomMessageDuration = 2.0f;

    [Header("Slot Change Confirm Popup")]
    [SerializeField] private GameObject slotChangeConfirmPanel;
    [SerializeField] private TMP_Text slotChangeConfirmText;
    [SerializeField] private Button slotChangeConfirmButton;
    [SerializeField] private Button slotChangeCancelButton;

    [Header("Game UI Root")]
    [SerializeField] private GameObject gamePanel;

    [SerializeField] private Color unselectedModeButtonColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color selectedModeButtonColor = new Color(0.75f, 1f, 0.35f, 1f);

    private readonly List<GameObject> roomSlotObjects = new();

    private RoomSlotTeam pendingSlotChangeTeam;
    private byte pendingSlotChangeIndex;

    private Coroutine roomMessageCoroutine;

    private uint pendingSwapRequesterPlayerId = 0;
    private uint selectedAdminTargetPlayerId = 0;
    private string selectedAdminTargetNickname = "";

    private RoomStateData currentRoomState;
    private ProfilePanelController _profilePanelController;

    private enum AfterRoomLeaveAction
    {
        OpenRoomList,
        EnterNormal,
        EnterRank
    }

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

    private AfterRoomLeaveAction afterRoomLeaveAction = AfterRoomLeaveAction.OpenRoomList;

    private GamePanelView currentGamePanelView = GamePanelView.None;
    private GameMode currentGameMode = GameMode.Normal;
    private GameMode previousModeBeforeCustom = GameMode.Normal;

    private void Start()
    {
        NetworkManager.Instance.SetGamePanelController(this);
        _profilePanelController = GetComponent<ProfilePanelController>();

        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(false);

        if (roomListPanel != null)
            roomListPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);



        if (slotSwapRequestPanel != null)
            slotSwapRequestPanel.SetActive(false);



        if (roomMessagePanel != null)
            roomMessagePanel.SetActive(false);

        if (roomPanelUI != null)
        {
            roomPanelUI.HideRoom();
            roomPanelUI.OnConfirmLeave = OnConfirmRoomLeave;
            roomPanelUI.OnGameStartClicked = OnClickRoomGameStart;
            roomPanelUI.OnSlotClicked = OnClickRoomSlot;
            roomPanelUI.OnSlotRightClicked = OnRightClickRoomSlot;
        }

        if (slotSwapAcceptButton != null)
        {
            slotSwapAcceptButton.onClick.RemoveAllListeners();
            slotSwapAcceptButton.onClick.AddListener(OnClickAcceptSlotSwap);
        }

        if (slotSwapRejectButton != null)
        {
            slotSwapRejectButton.onClick.RemoveAllListeners();
            slotSwapRejectButton.onClick.AddListener(OnClickRejectSlotSwap);
        }

        if (slotChangeConfirmPanel != null)
            slotChangeConfirmPanel.SetActive(false);

        if (slotChangeConfirmButton != null)
        {
            slotChangeConfirmButton.onClick.RemoveAllListeners();
            slotChangeConfirmButton.onClick.AddListener(OnClickConfirmSlotChange);
        }

        if (slotChangeCancelButton != null)
        {
            slotChangeCancelButton.onClick.RemoveAllListeners();
            slotChangeCancelButton.onClick.AddListener(OnClickCancelSlotChange);
        }

        if (slotAdminMenuPanel != null)
            slotAdminMenuPanel.SetActive(false);

        if (transferLeaderButton != null)
        {
            transferLeaderButton.onClick.RemoveAllListeners();
            transferLeaderButton.onClick.AddListener(OnClickTransferLeader);
        }

        if (kickButton != null)
        {
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(OnClickKickMember);
        }

        if (roomRefreshButton != null)
        {
            roomRefreshButton.onClick.RemoveAllListeners();
            roomRefreshButton.onClick.AddListener(
                OnClickRefreshRoomList
            );
        }
        else
        {
            Debug.LogWarning(
                "[ROOM LIST] 방 조회 버튼이 연결되지 않았습니다."
            );
        }

        HideAllTempGameSlots();

        bool returningFromResult =
            NetworkManager.Instance.HasPendingReturnRoom;

        if (returningFromResult)
        {
            SelectGameMode(GameMode.Custom);

            uint roomId =
                NetworkManager.Instance.PendingReturnRoomId;

            NetworkManager.Instance.ClearPendingReturnRoom();

            // 결과 화면에서 돌아올 때는 게임 메뉴 상태를 유지한다.
            GetComponent<NavigationPanelController>().SelectLobbyMainMenu(LobbyMainMenu.Game);

            NetworkManager.Instance.SendRoomStateRequest(roomId);

            Debug.Log(
                $"[LOBBY RETURN FROM RESULT] " +
                $"request room state. roomId={roomId}"
            );
        }
        else
        {
            SelectGameMode(GameMode.Normal);

            // 최초 로비 입장 시 로비 버튼만 선택 이미지로 표시한다.
            GetComponent<NavigationPanelController>().SelectLobbyMainMenu(LobbyMainMenu.Lobby);
        }
    }

    private void Update()
    {
        //if (roomPanelUI != null && roomPanelUI.IsRoomOpen())
        //{
        //    if (roomPanelUI.IsLeaveConfirmOpen())
        //    {
        //        roomPanelUI.HideLeaveConfirmPanel();
        //    }
        //    else
        //    {
        //        afterRoomLeaveAction = AfterRoomLeaveAction.OpenRoomList;
        //        roomPanelUI.ShowLeaveConfirmPanel();
        //    }

        //    return;
        //}
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
        if (gamePanel == null)
            return;

        // 서버 응답이 늦게 도착해도 다른 메인 메뉴 위에 게임 패널을 강제로 띄우지 않는다.
        gamePanel.SetActive(GetComponent<NavigationPanelController>().currentLobbyMainMenu == LobbyMainMenu.Game);
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

    private void FillLocalTempGameSlot0()
    {
        if (tempGamePlayerSlots == null || tempGamePlayerSlots.Length == 0)
            return;

        TempGamePlayerSlotUI slot0 = tempGamePlayerSlots[0];
        if (slot0 == null)
            return;

        Sprite icon = GetSafeSprite(tempRoomIconSprites, _profilePanelController.currentplayerIconId);
        Sprite border = GetSafeSprite(tempRoomBorderSprites, 0);
        Sprite rank = GetSafeSprite(tempRoomRankSprites, 0);

        slot0.SetData(icon, border, rank, _profilePanelController.currentNickname);

        for (int i = 1; i < tempGamePlayerSlots.Length; i++)
        {
            if (tempGamePlayerSlots[i] != null)
                tempGamePlayerSlots[i].Hide();
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

    private void EnterTempGameRoom()
    {
        ShowTempGameRoomPanel();
        HideAllTempGameSlots();
        FillLocalTempGameSlot0();
        NetworkManager.Instance.SendTempGameRoomEnter();
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

    private void ShowCurrentGamePanelView()
    {
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

        if (currentGamePanelView == GamePanelView.RoomList)
        {
            ShowRoomListPanel();
            return;
        }

        if (currentGamePanelView == GamePanelView.RoomPanel)
        {
            ShowRoomPanelOnly();
        }
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

    public void OnClickRefreshRoomList()
    {
        Debug.Log("[ROOM LIST] RefreshButton Click");

        if (NetworkManager.Instance == null)
        {
            Debug.LogError(
                "[ROOM LIST] NetworkManager.Instance is null"
            );

            return;
        }

        // 현재 방 안에 들어가 있는 상태라면
        // 방 목록으로 강제로 넘어가지 않는다.
        if (IsInCustomRoom())
        {
            ShowRoomMessage(
                "방에 들어가 있는 동안에는 방 목록을 조회할 수 없습니다."
            );

            return;
        }

        // 메인 게임 메뉴가 닫혀 있어도 방 목록 화면을 열어 준다.
        GetComponent<NavigationPanelController>().SelectLobbyMainMenu(
            LobbyMainMenu.Game
        );

        if (currentGameMode != GameMode.Custom)
        {
            previousModeBeforeCustom =
                currentGameMode;

            SelectGameMode(
                GameMode.Custom
            );
        }

        ShowRoomListPanel();

        // 현재 서버에 존재하는 방 목록을 새로 요청한다.
        NetworkManager.Instance.SendRoomList();

        Debug.Log(
            "[ROOM LIST] room list request sent"
        );
    }

    public void OnClickUserRoomButton()
    {
        Debug.Log("[ROOM LIST] UserRoomButton Click");

        if (NetworkManager.Instance == null)
        {
            Debug.LogError(
                "[ROOM LIST] NetworkManager.Instance is null"
            );

            return;
        }

        if (IsInCustomRoom())
        {
            ShowRoomMessage(
                "현재 방에 들어가 있습니다."
            );

            return;
        }

        // 이미 사설방 모드여도 종료하지 않고
        // 현재 방 목록을 다시 요청한다.
        if (currentGameMode == GameMode.Custom)
        {
            ShowRoomListPanel();
            NetworkManager.Instance.SendRoomList();

            Debug.Log(
                "[ROOM LIST] custom mode refresh request"
            );

            return;
        }

        previousModeBeforeCustom =
            currentGameMode;

        SelectGameMode(
            GameMode.Custom
        );

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

            Button button = slotObj.GetComponent<Button>();
            if (button == null)
                button = slotObj.GetComponentInChildren<Button>(true);

            if (button != null)
            {
                uint roomId = room.roomId;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    Debug.Log($"[ROOM SLOT CLICK] roomId={roomId}");
                    NetworkManager.Instance.SendRoomEnter(roomId);
                });
            }
            else
            {
                Debug.LogWarning("[ROOM LIST UI] RoomSlot prefab has no Button component");
            }
        }
    }

    public void OnRecvRoomEnterResult(byte result, uint roomId)
    {
        Debug.Log($"[ROOM ENTER UI] result={result}, roomId={roomId}");

        if (result == (byte)RoomEnterResult.SUCCESS)
        {
            Debug.Log("[ROOM ENTER UI] success. wait for room state packet");
            return;
        }

        string msg = result switch
        {
            (byte)RoomEnterResult.ROOM_NOT_FOUND => "방을 찾을 수 없습니다.",
            (byte)RoomEnterResult.ROOM_FULL => "방이 가득 찼습니다.",
            (byte)RoomEnterResult.ALREADY_IN_ROOM => "이미 방에 들어가 있습니다.",
            (byte)RoomEnterResult.PLAYER_DATA_NOT_FOUND => "플레이어 정보를 찾을 수 없습니다.",
            (byte)RoomEnterResult.NOT_IN_LOBBY => "로비 상태가 아닙니다.",
            _ => "방 입장에 실패했습니다."
        };

        Debug.LogWarning($"[ROOM ENTER UI] fail: {msg}");

        ShowRoomListPanel();
        NetworkManager.Instance.SendRoomList();
    }

    public void OnRecvRoomState(RoomStateData roomState)
    {
        Debug.Log(
            $"[ROOM STATE UI] " +
            $"roomId={roomState.roomId}, " +
            $"roomName={roomState.roomName}, " +
            $"memberCount={roomState.members.Count}"
        );

        bool myLeader = false;

        for (int i = 0; i < roomState.members.Count; i++)
        {
            RoomMemberData member = roomState.members[i];

            if (member.nickname == _profilePanelController.currentNickname && member.isLeader)
            {
                myLeader = true;
                break;
            }
        }

        roomState.isLeader = myLeader;

        currentRoomState = roomState;
        currentGamePanelView = GamePanelView.RoomPanel;

        SelectGameMode(GameMode.Custom);

        ShowGamePanel();

        if (roomPanelUI != null)
        {
            roomPanelUI.OpenRoom(roomState);
        }
        else
        {
            Debug.LogWarning("[ROOM STATE UI] roomPanelUI is null");
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
        /*
         * 게임 내부 상태는 유지하고 메인 로비 화면으로만 돌아간다.
         * 이후 GameButton을 다시 누르면 currentGamePanelView 상태가 복원된다.
         */
        GetComponent<NavigationPanelController>().SelectLobbyMainMenu(LobbyMainMenu.Lobby);
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

    private void OnClickRoomSlot(RoomSlotTeam team, byte slotIndex)
    {
        Debug.Log($"[ROOM SLOT CLICK] team={team}, slot={slotIndex}");

        if (currentGamePanelView != GamePanelView.RoomPanel || currentRoomState == null)
        {
            Debug.LogWarning("[ROOM SLOT CLICK] not in room panel");
            return;
        }

        RoomMemberData targetMember = FindRoomMemberBySlot(team, slotIndex);

        pendingSlotChangeTeam = team;
        pendingSlotChangeIndex = slotIndex;

        if (slotChangeConfirmText != null)
        {
            if (targetMember == null)
            {
                slotChangeConfirmText.text = "이 자리로 이동하시겠습니까?";
            }
            else
            {
                slotChangeConfirmText.text =
                    targetMember.nickname + "님과 자리 변경을 요청하시겠습니까?";
            }
        }

        if (slotChangeConfirmPanel != null)
            slotChangeConfirmPanel.SetActive(true);
    }

    public void OnRecvRoomSlotChangeResult(
        byte result,
        RoomSlotTeam targetTeam,
        byte targetSlotIndex,
        string targetNickname)
    {
        Debug.Log(
            $"[ROOM SLOT CHANGE UI] " +
            $"result={result}, targetTeam={targetTeam}, slot={targetSlotIndex}, targetNickname={targetNickname}"
        );

        if (result == (byte)RoomSlotChangeResult.SUCCESS)
        {
            ShowRoomMessage("자리를 변경했습니다.");
            return;
        }

        if (result == (byte)RoomSlotChangeResult.REQUEST_SENT)
        {
            ShowRoomMessage(targetNickname + "님에게 자리 변경 요청을 보냈습니다.");
            return;
        }

        if (result == (byte)RoomSlotChangeResult.REJECTED)
        {
            ShowRoomMessage(targetNickname + "님이 자리 변경 요청을 거절했습니다.");
            return;
        }

        if (result == (byte)RoomSlotChangeResult.SAME_SLOT)
        {
            ShowRoomMessage("이미 해당 자리에 있습니다.");
            return;
        }

        if (result == (byte)RoomSlotChangeResult.REQUEST_ALREADY_EXISTS)
        {
            ShowRoomMessage("상대방에게 이미 처리 중인 요청이 있습니다.");
            return;
        }

        ShowRoomMessage("자리 변경에 실패했습니다.");
    }

    public void OnRecvRoomSlotSwapRequest(RoomSlotSwapRequestData data)
    {
        Debug.Log(
            $"[ROOM SLOT SWAP POPUP] requester={data.requesterNickname}, requesterId={data.requesterPlayerId}"
        );

        pendingSwapRequesterPlayerId = data.requesterPlayerId;

        if (slotSwapRequestText != null)
        {
            slotSwapRequestText.text =
                data.requesterNickname + "님과 자리를 변경하시겠습니까?";
        }

        if (slotSwapRequestPanel != null)
            slotSwapRequestPanel.SetActive(true);
    }

    private void OnClickAcceptSlotSwap()
    {
        Debug.Log($"[ROOM SLOT SWAP ACCEPT] requester={pendingSwapRequesterPlayerId}");

        if (pendingSwapRequesterPlayerId == 0)
            return;

        NetworkManager.Instance.SendRoomSlotSwapResponse(
            pendingSwapRequesterPlayerId,
            true
        );

        pendingSwapRequesterPlayerId = 0;

        if (slotSwapRequestPanel != null)
            slotSwapRequestPanel.SetActive(false);
    }

    private void OnClickRejectSlotSwap()
    {
        Debug.Log($"[ROOM SLOT SWAP REJECT] requester={pendingSwapRequesterPlayerId}");

        if (pendingSwapRequesterPlayerId == 0)
            return;

        NetworkManager.Instance.SendRoomSlotSwapResponse(
            pendingSwapRequesterPlayerId,
            false
        );

        pendingSwapRequesterPlayerId = 0;

        if (slotSwapRequestPanel != null)
            slotSwapRequestPanel.SetActive(false);
    }

    private void ShowRoomMessage(string message)
    {
        Debug.Log("[ROOM MESSAGE] " + message);

        if (roomMessageText != null)
            roomMessageText.text = message;

        if (roomMessagePanel != null)
            roomMessagePanel.SetActive(true);

        if (roomMessageCoroutine != null)
            StopCoroutine(roomMessageCoroutine);

        roomMessageCoroutine = StartCoroutine(HideRoomMessageAfterSeconds());
    }

    private IEnumerator HideRoomMessageAfterSeconds()
    {
        yield return new WaitForSeconds(roomMessageDuration);

        if (roomMessagePanel != null)
            roomMessagePanel.SetActive(false);

        roomMessageCoroutine = null;
    }

    private RoomMemberData FindRoomMemberBySlot(RoomSlotTeam team, byte slotIndex)
    {
        if (currentRoomState == null || currentRoomState.members == null)
            return null;

        for (int i = 0; i < currentRoomState.members.Count; i++)
        {
            RoomMemberData member = currentRoomState.members[i];

            if (member.team == team && member.slotIndex == slotIndex)
                return member;
        }

        return null;
    }

    private void OnClickConfirmSlotChange()
    {
        Debug.Log(
            $"[ROOM SLOT CHANGE CONFIRM] " +
            $"team={pendingSlotChangeTeam}, slot={pendingSlotChangeIndex}"
        );

        if (slotChangeConfirmPanel != null)
            slotChangeConfirmPanel.SetActive(false);

        NetworkManager.Instance.SendRoomSlotChange(
            pendingSlotChangeTeam,
            pendingSlotChangeIndex
        );
    }

    private void OnClickCancelSlotChange()
    {
        Debug.Log("[ROOM SLOT CHANGE CANCEL]");

        if (slotChangeConfirmPanel != null)
            slotChangeConfirmPanel.SetActive(false);
    }

    private void OnRightClickRoomSlot(RoomSlotTeam team, byte slotIndex)
    {
        Debug.Log($"[ROOM ADMIN RIGHT CLICK] team={team}, slot={slotIndex}");

        if (currentGamePanelView != GamePanelView.RoomPanel || currentRoomState == null)
            return;

        if (!currentRoomState.isLeader)
        {
            ShowRoomMessage("방장만 사용할 수 있습니다.");
            return;
        }

        RoomMemberData targetMember = FindRoomMemberBySlot(team, slotIndex);
        if (targetMember == null)
        {
            HideSlotAdminMenu();
            return;
        }

        if (targetMember.nickname == _profilePanelController.currentNickname)
        {
            ShowRoomMessage("자기 자신에게는 사용할 수 없습니다.");
            HideSlotAdminMenu();
            return;
        }

        selectedAdminTargetPlayerId = targetMember.playerId;
        selectedAdminTargetNickname = targetMember.nickname;

        if (slotAdminTargetText != null)
            slotAdminTargetText.text = targetMember.nickname;

        if (slotAdminMenuPanel != null)
        {
            slotAdminMenuPanel.SetActive(true);
            slotAdminMenuPanel.transform.position = Input.mousePosition;
        }
    }

    private void HideSlotAdminMenu()
    {
        selectedAdminTargetPlayerId = 0;
        selectedAdminTargetNickname = "";

        if (slotAdminMenuPanel != null)
            slotAdminMenuPanel.SetActive(false);
    }

    private void OnClickTransferLeader()
    {
        if (selectedAdminTargetPlayerId == 0)
            return;

        Debug.Log($"[ROOM ADMIN] transfer leader target={selectedAdminTargetPlayerId}");

        NetworkManager.Instance.SendRoomTransferLeader(selectedAdminTargetPlayerId);

        HideSlotAdminMenu();
    }

    private void OnClickKickMember()
    {
        if (selectedAdminTargetPlayerId == 0)
            return;

        Debug.Log($"[ROOM ADMIN] kick target={selectedAdminTargetPlayerId}");

        NetworkManager.Instance.SendRoomKick(selectedAdminTargetPlayerId);

        HideSlotAdminMenu();
    }

    public void OnRecvRoomTransferLeaderResult(
    byte result,
    uint targetPlayerId,
    string targetNickname)
    {
        Debug.Log(
            $"[ROOM TRANSFER LEADER UI] result={result}, target={targetPlayerId}, nickname={targetNickname}"
        );

        if (result == (byte)RoomAdminResult.SUCCESS)
        {
            ShowRoomMessage(targetNickname + "님에게 방장을 넘겼습니다.");
            return;
        }

        if (result == (byte)RoomAdminResult.NOT_LEADER)
        {
            ShowRoomMessage("방장만 사용할 수 있습니다.");
            return;
        }

        if (result == (byte)RoomAdminResult.CANNOT_TARGET_SELF)
        {
            ShowRoomMessage("자기 자신에게는 사용할 수 없습니다.");
            return;
        }

        ShowRoomMessage("방장 넘기기에 실패했습니다.");
    }

    public void OnRecvRoomKickResult(
        byte result,
        uint targetPlayerId,
        string targetNickname)
    {
        Debug.Log(
            $"[ROOM KICK UI] result={result}, target={targetPlayerId}, nickname={targetNickname}"
        );

        if (result == (byte)RoomAdminResult.SUCCESS)
        {
            ShowRoomMessage(targetNickname + "님을 강퇴했습니다.");
            return;
        }

        if (result == (byte)RoomAdminResult.NOT_LEADER)
        {
            ShowRoomMessage("방장만 사용할 수 있습니다.");
            return;
        }

        if (result == (byte)RoomAdminResult.CANNOT_TARGET_SELF)
        {
            ShowRoomMessage("자기 자신은 강퇴할 수 없습니다.");
            return;
        }

        ShowRoomMessage("강퇴에 실패했습니다.");
    }

    public void OnRecvRoomKicked(uint roomId, string kickerNickname)
    {
        Debug.Log($"[ROOM KICKED UI] roomId={roomId}, kicker={kickerNickname}");

        currentRoomState = null;
        currentGamePanelView = GamePanelView.RoomList;

        if (roomPanelUI != null)
            roomPanelUI.HideRoom();

        SelectGameMode(GameMode.Custom);

        ShowRoomListPanel();
        NetworkManager.Instance.SendRoomList();

        ShowRoomMessage(kickerNickname + "님에 의해 방에서 강퇴되었습니다.");
    }

    private void OnClickRoomGameStart()
    {
        Debug.Log("[ROOM GAME START] button click");

        if (currentRoomState == null)
        {
            ShowRoomMessage("방 정보가 없습니다.");
            return;
        }

        if (!currentRoomState.isLeader)
        {
            ShowRoomMessage("방장만 게임을 시작할 수 있습니다.");
            return;
        }

        if (!currentRoomState.canStart)
        {
            ShowRoomMessage("A팀 3명, B팀 3명이 있어야 시작할 수 있습니다.");
            return;
        }

        NetworkManager.Instance.SendRoomGameStart();
    }

    public void OnRecvRoomGameStartResult(byte result)
    {
        Debug.Log($"[ROOM GAME START UI] result={result}");

        if (result == (byte)RoomGameStartResult.SUCCESS)
        {
            SceneManager.LoadScene("Game_TestVersion");
            return;
        }

        if (result == (byte)RoomGameStartResult.NOT_LEADER)
        {
            ShowRoomMessage("방장만 게임을 시작할 수 있습니다.");
            return;
        }

        if (result == (byte)RoomGameStartResult.NOT_ALL_RETURNED_FROM_RESULT)
        {
            ShowRoomMessage("룸으로 돌아오지 못한 플레이어가 있습니다.");
            return;
        }

        if (result == (byte)RoomGameStartResult.NOT_ENOUGH_PLAYERS)
        {
            ShowRoomMessage("A팀 3명, B팀 3명이 있어야 시작할 수 있습니다.");
            return;
        }

        if (result == (byte)RoomGameStartResult.ALREADY_PLAYING)
        {
            ShowRoomMessage("이미 게임이 시작되었습니다.");
            return;
        }

        ShowRoomMessage("게임 시작에 실패했습니다.");
    }
}
