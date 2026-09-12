using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LobbyRoomChatKind : byte
{
    None = 0,
    ReadyRoom = 1,
    PrivateRoom = 2
}

public class ChatPanelController : MonoBehaviour
{
    private enum ChatChannel
    {
        Lobby,
        Room,
        Friends
    }

    private sealed class ChatMessage
    {
        public uint PlayerId;
        public string Nickname;
        public string Message;
    }

    private sealed class PendingFriendEcho
    {
        public uint PeerPlayerId;
        public string Message;
    }

    private const int MaxMessagesPerChannel = 100;

    [Header("Chat")]
    [SerializeField] private GameObject ChatPrefeb;
    [SerializeField] private Transform ChatSlotContent;
    [SerializeField] private TMP_InputField ChatInputField;

    [Header("Chat Panel")]
    [SerializeField] private GameObject _chatPanel;
    [SerializeField] private GameObject _chatButton;

    [Header("Chat Navigation")]
    [SerializeField] private Button lobbyTabButton;
    [SerializeField] private Button roomTabButton;
    [SerializeField] private Button friendsTabButton;
    [SerializeField] private TMP_Text roomTabLabel;
    [SerializeField] private Sprite tabNormalSprite;
    [SerializeField] private Sprite tabSelectedSprite;
    [SerializeField] private GameObject friendTools;
    [SerializeField] private GameObject friendsConversationPanel;
    [SerializeField] private GameObject chatScrollObject;
    [SerializeField] private Button sendButton;

    [Header("Friend Chat Selector")]
    [SerializeField] private Transform friendSelectorContent;
    [SerializeField] private TMP_Text friendSelectorEmptyText;
    [SerializeField] private TMP_Text friendSelectorHeaderText;
    [SerializeField] private Button friendSelectorRefreshButton;
    [SerializeField] private GameObject friendListItemPrefab;
    [SerializeField] private Sprite[] friendProfileIcons;
    [SerializeField] private Sprite[] friendRankIcons;

    private readonly List<ChatMessage> lobbyMessages = new();
    private readonly Dictionary<string, List<ChatMessage>> roomMessages = new();
    private readonly Dictionary<uint, List<ChatMessage>> friendMessages = new();
    private readonly List<PendingFriendEcho> pendingFriendEchoes = new();
    private readonly List<FriendUserData> friendUsers = new();
    private readonly List<FriendListItemUI> friendSelectorItems = new();

    private ChatChannel activeChannel = ChatChannel.Lobby;
    private LobbyRoomChatKind roomChatKind = LobbyRoomChatKind.None;
    private uint roomChatId;
    private uint friendChatTargetId;
    private string friendChatTargetUserId = "";
    private string friendChatTargetNickname = "";

    private ScrollRect chatScrollRect;
    private RectTransform chatScrollRectTransform;
    private RectTransform chatInputRectTransform;
    private RectTransform sendButtonRectTransform;
    private Vector2 defaultChatScrollPosition;
    private Vector2 defaultChatScrollSize;
    private Vector2 defaultChatInputPosition;
    private Vector2 defaultChatInputSize;
    private Vector2 defaultSendButtonPosition;

    private void Start()
    {
        if (tabNormalSprite == null)
            tabNormalSprite = Resources.Load<Sprite>("Chat/ChatTabNormal");

        if (tabSelectedSprite == null)
            tabSelectedSprite = Resources.Load<Sprite>("Chat/ChatTabSelected");

        BindChatControls();
        CaptureDefaultChatLayout();
        UpdateDirectTabPositions(roomChatKind != LobbyRoomChatKind.None);

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SetChatPanelController(this);

        SelectChannel(ChatChannel.Lobby);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.ClearChatPanelController(this);
    }

    public void SetRoomChatContext(
        LobbyRoomChatKind kind,
        uint roomId,
        string roomName)
    {
        roomChatKind = kind;
        roomChatId = roomId;

        bool hasRoom = roomChatKind != LobbyRoomChatKind.None;

        if (roomTabButton != null)
            roomTabButton.gameObject.SetActive(hasRoom);

        UpdateDirectTabPositions(hasRoom);

        if (roomTabLabel != null)
        {
            roomTabLabel.text = roomChatKind == LobbyRoomChatKind.PrivateRoom
                ? "사설방"
                : "준비방";
        }

        if (!hasRoom && activeChannel == ChatChannel.Room)
        {
            SelectChannel(ChatChannel.Lobby);
            return;
        }

        if (activeChannel == ChatChannel.Room)
            SelectChannel(ChatChannel.Room);
        else
            ApplyTabVisuals();
    }

    public void OnRecvLobbyChat(uint playerId, string nickname, string message)
    {
        bool historyTrimmed = AddMessage(
            lobbyMessages,
            playerId,
            nickname,
            message
        );

        if (activeChannel == ChatChannel.Lobby)
        {
            if (historyTrimmed)
                RenderActiveHistory();
            else
                AppendMessage(lobbyMessages[lobbyMessages.Count - 1]);
        }
    }

    public void OnRecvRoomChat(
        uint roomId,
        uint playerId,
        string nickname,
        string message)
    {
        if (roomChatKind == LobbyRoomChatKind.None)
            return;

        if (roomChatKind == LobbyRoomChatKind.PrivateRoom && roomId != roomChatId)
            return;

        if (roomChatKind == LobbyRoomChatKind.ReadyRoom && roomId != 0)
            return;

        List<ChatMessage> history = GetCurrentRoomHistory();
        bool historyTrimmed = AddMessage(
            history,
            playerId,
            nickname,
            message
        );

        if (activeChannel == ChatChannel.Room)
        {
            if (historyTrimmed)
                RenderActiveHistory();
            else
                AppendMessage(history[history.Count - 1]);
        }
    }

    public void OnRecvFriendChat(
        uint senderPlayerId,
        uint targetPlayerId,
        string senderNickname,
        string message)
    {
        if (NetworkManager.Instance == null)
            return;

        uint peerPlayerId = senderPlayerId == NetworkManager.Instance.MyPlayerId
            ? targetPlayerId
            : senderPlayerId;

        if (peerPlayerId == 0)
            return;

        if (senderPlayerId == NetworkManager.Instance.MyPlayerId &&
            ConsumePendingFriendEcho(peerPlayerId, message))
        {
            return;
        }

        List<ChatMessage> history = GetFriendHistory(peerPlayerId);
        bool historyTrimmed = AddMessage(
            history,
            senderPlayerId,
            senderNickname,
            message
        );

        if (activeChannel == ChatChannel.Friends &&
            friendChatTargetId == peerPlayerId)
        {
            if (historyTrimmed)
                RenderActiveHistory();
            else
                AppendMessage(history[history.Count - 1]);
        }
    }

    public void SetFriendChatTarget(uint playerId, string nickname)
    {
        friendChatTargetId = playerId;
        friendChatTargetUserId = "";
        friendChatTargetNickname = nickname ?? "";
        RenderFriendSelector();

        if (activeChannel == ChatChannel.Friends)
            SelectChannel(ChatChannel.Friends);
    }

    public void OpenFriendChat(uint playerId, string nickname)
    {
        if (playerId == 0)
            return;

        SetFriendChatTarget(playerId, nickname);
        SelectChannel(ChatChannel.Friends);

        if (_chatPanel != null && !_chatPanel.activeSelf)
        {
            UIAnimator panelAnimator = _chatPanel.GetComponent<UIAnimator>();
            if (panelAnimator != null)
                panelAnimator.Show();
            else
                _chatPanel.SetActive(true);

            UIAnimator buttonAnimator = _chatButton != null
                ? _chatButton.GetComponent<UIAnimator>()
                : null;
            if (buttonAnimator != null)
                buttonAnimator.Close();
            else if (_chatButton != null)
                _chatButton.SetActive(false);
        }
    }

    public void ClearFriendChatTarget()
    {
        friendChatTargetId = 0;
        friendChatTargetUserId = "";
        friendChatTargetNickname = "";
        RenderFriendSelector();

        if (activeChannel == ChatChannel.Friends)
            SelectChannel(ChatChannel.Friends);
    }

    public void OnClickLobbyChat()
    {
        SendCurrentChannelMessage();
    }

    public void OnClickLobbyTab()
    {
        SelectChannel(ChatChannel.Lobby);
    }

    public void OnClickRoomTab()
    {
        if (roomChatKind != LobbyRoomChatKind.None)
            SelectChannel(ChatChannel.Room);
    }

    public void OnClickFriendsTab()
    {
        SelectChannel(ChatChannel.Friends);
        RequestFriendOverview();
    }

    public void ApplyFriendOverview(List<FriendUserData> friends)
    {
        friendUsers.Clear();
        if (friends != null)
            friendUsers.AddRange(friends);

        friendUsers.Sort((left, right) =>
        {
            int leftOffline = left.presenceStatus == FriendPresenceStatus.Offline ? 1 : 0;
            int rightOffline = right.presenceStatus == FriendPresenceStatus.Offline ? 1 : 0;
            int statusOrder = leftOffline.CompareTo(rightOffline);
            if (statusOrder != 0)
                return statusOrder;

            return string.Compare(GetFriendDisplayName(left), GetFriendDisplayName(right), System.StringComparison.CurrentCultureIgnoreCase);
        });

        if (!string.IsNullOrEmpty(friendChatTargetUserId))
        {
            FriendUserData current = friendUsers.Find(friend => friend.userId == friendChatTargetUserId);
            if (current != null)
                friendChatTargetId = current.playerId;
        }

        RenderFriendSelector();

        if (activeChannel == ChatChannel.Friends)
            SelectChannel(ChatChannel.Friends);
    }

    public void OnChatPanel()
    {
        if (_chatPanel == null || _chatButton == null)
            return;

        UIAnimator panelAnimator = _chatPanel.GetComponent<UIAnimator>();
        UIAnimator buttonAnimator = _chatButton.GetComponent<UIAnimator>();

        if (_chatPanel.activeSelf)
        {
            if (panelAnimator != null)
                panelAnimator.Close();
            else
                _chatPanel.SetActive(false);

            if (buttonAnimator != null)
                buttonAnimator.Show();
            else
                _chatButton.SetActive(true);
        }
        else
        {
            if (panelAnimator != null)
                panelAnimator.Show();
            else
                _chatPanel.SetActive(true);

            if (buttonAnimator != null)
                buttonAnimator.Close();
            else
                _chatButton.SetActive(false);
        }
    }

    private void BindChatControls()
    {
        if (ChatInputField != null)
        {
            ChatInputField.characterLimit = 120;
            ChatInputField.onSubmit.RemoveListener(OnSubmitChat);
            ChatInputField.onSubmit.AddListener(OnSubmitChat);
        }

        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(SendCurrentChannelMessage);
        }

        BindTabButton(lobbyTabButton, OnClickLobbyTab);
        BindTabButton(roomTabButton, OnClickRoomTab);
        BindTabButton(friendsTabButton, OnClickFriendsTab);

        if (friendSelectorRefreshButton != null)
        {
            friendSelectorRefreshButton.onClick.RemoveAllListeners();
            friendSelectorRefreshButton.onClick.AddListener(RequestFriendOverview);

            TMP_Text refreshLabel = friendSelectorRefreshButton.GetComponentInChildren<TMP_Text>(true);
            if (refreshLabel != null)
            {
                refreshLabel.text = "↻";
                refreshLabel.fontSize = 20f;
            }
        }

        if (chatScrollObject != null)
            chatScrollRect = chatScrollObject.GetComponent<ScrollRect>();
    }

    private void CaptureDefaultChatLayout()
    {
        chatScrollRectTransform = chatScrollObject != null
            ? chatScrollObject.GetComponent<RectTransform>()
            : null;
        chatInputRectTransform = ChatInputField != null
            ? ChatInputField.GetComponent<RectTransform>()
            : null;
        sendButtonRectTransform = sendButton != null
            ? sendButton.GetComponent<RectTransform>()
            : null;

        if (chatScrollRectTransform != null)
        {
            defaultChatScrollPosition = chatScrollRectTransform.anchoredPosition;
            defaultChatScrollSize = chatScrollRectTransform.sizeDelta;
        }

        if (chatInputRectTransform != null)
        {
            defaultChatInputPosition = chatInputRectTransform.anchoredPosition;
            defaultChatInputSize = chatInputRectTransform.sizeDelta;
        }

        if (sendButtonRectTransform != null)
            defaultSendButtonPosition = sendButtonRectTransform.anchoredPosition;

        if (friendSelectorHeaderText != null)
        {
            RectTransform headerRect = friendSelectorHeaderText.rectTransform;
            headerRect.anchorMin = new Vector2(0.06f, 0.12f);
            headerRect.anchorMax = new Vector2(0.73f, 0.88f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = Vector2.zero;
            friendSelectorHeaderText.alignment = TextAlignmentOptions.Center;
            friendSelectorHeaderText.fontSize = 16f;
        }
    }

    private void RequestFriendOverview()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendFriendOverviewRequest();
    }

    private static void BindTabButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void OnSubmitChat(string _)
    {
        SendCurrentChannelMessage();
    }

    private void SendCurrentChannelMessage()
    {
        if (ChatInputField == null || NetworkManager.Instance == null)
            return;

        string message = ChatInputField.text.Trim();
        if (message.Length == 0)
            return;

        if (activeChannel == ChatChannel.Lobby)
        {
            NetworkManager.Instance.SendLobbyChat(message);
        }
        else if (activeChannel == ChatChannel.Room &&
                 roomChatKind != LobbyRoomChatKind.None)
        {
            NetworkManager.Instance.SendRoomChat(message);
        }
        else if (activeChannel == ChatChannel.Friends &&
                 friendChatTargetId != 0)
        {
            NetworkManager.Instance.SendFriendChat(
                friendChatTargetId,
                message
            );
            AppendOwnFriendMessage(friendChatTargetId, message);
        }
        else
        {
            return;
        }

        ChatInputField.text = "";
        ChatInputField.ActivateInputField();
    }

    private void AppendOwnFriendMessage(uint peerPlayerId, string message)
    {
        if (NetworkManager.Instance == null || peerPlayerId == 0)
            return;

        pendingFriendEchoes.Add(new PendingFriendEcho
        {
            PeerPlayerId = peerPlayerId,
            Message = message
        });

        if (pendingFriendEchoes.Count > MaxMessagesPerChannel)
            pendingFriendEchoes.RemoveAt(0);

        List<ChatMessage> history = GetFriendHistory(peerPlayerId);
        bool historyTrimmed = AddMessage(
            history,
            NetworkManager.Instance.MyPlayerId,
            "나",
            message
        );

        if (activeChannel == ChatChannel.Friends &&
            friendChatTargetId == peerPlayerId)
        {
            if (historyTrimmed)
                RenderActiveHistory();
            else
                AppendMessage(history[history.Count - 1]);
        }
    }

    private bool ConsumePendingFriendEcho(uint peerPlayerId, string message)
    {
        for (int i = 0; i < pendingFriendEchoes.Count; i++)
        {
            PendingFriendEcho pending = pendingFriendEchoes[i];
            if (pending.PeerPlayerId != peerPlayerId ||
                pending.Message != message)
            {
                continue;
            }

            pendingFriendEchoes.RemoveAt(i);
            return true;
        }

        return false;
    }

    private bool AddMessage(
        List<ChatMessage> history,
        uint playerId,
        string nickname,
        string message)
    {
        bool historyTrimmed = false;

        if (history.Count >= MaxMessagesPerChannel)
        {
            history.RemoveAt(0);
            historyTrimmed = true;
        }

        history.Add(new ChatMessage
        {
            PlayerId = playerId,
            Nickname = nickname ?? "",
            Message = message ?? ""
        });

        return historyTrimmed;
    }

    private void AppendMessage(ChatMessage message)
    {
        if (ChatPrefeb == null || ChatSlotContent == null)
            return;

        GameObject item = Instantiate(ChatPrefeb, ChatSlotContent);
        TMP_Text text = item.GetComponentInChildren<TMP_Text>();

        if (text != null)
            text.text = message.Nickname + " : " + message.Message;

        ScrollToLatestMessage();
    }

    private void RenderActiveHistory()
    {
        ClearVisibleMessages();

        List<ChatMessage> history;

        if (activeChannel == ChatChannel.Lobby)
            history = lobbyMessages;
        else if (activeChannel == ChatChannel.Room)
            history = GetCurrentRoomHistory();
        else
            history = GetFriendHistory(friendChatTargetId);

        for (int i = 0; i < history.Count; i++)
            AppendMessage(history[i]);

        ScrollToLatestMessage();
    }

    private void ClearVisibleMessages()
    {
        if (ChatSlotContent == null)
            return;

        for (int i = ChatSlotContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = ChatSlotContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private List<ChatMessage> GetCurrentRoomHistory()
    {
        string key = roomChatKind == LobbyRoomChatKind.PrivateRoom
            ? "private:" + roomChatId
            : "ready";

        if (!roomMessages.TryGetValue(key, out List<ChatMessage> history))
        {
            history = new List<ChatMessage>();
            roomMessages.Add(key, history);
        }

        return history;
    }

    private List<ChatMessage> GetFriendHistory(uint playerId)
    {
        if (!friendMessages.TryGetValue(
                playerId,
                out List<ChatMessage> history))
        {
            history = new List<ChatMessage>();
            friendMessages.Add(playerId, history);
        }

        return history;
    }

    private void SelectChannel(ChatChannel channel)
    {
        if (channel == ChatChannel.Room &&
            roomChatKind == LobbyRoomChatKind.None)
        {
            channel = ChatChannel.Lobby;
        }

        activeChannel = channel;
        bool isFriends = activeChannel == ChatChannel.Friends;
        bool hasFriendTarget = isFriends && friendChatTargetId != 0;
        bool showMessages = !isFriends || hasFriendTarget;

        if (chatScrollObject != null)
            chatScrollObject.SetActive(showMessages);

        if (ChatInputField != null)
            ChatInputField.gameObject.SetActive(showMessages);

        if (sendButton != null)
            sendButton.gameObject.SetActive(showMessages);

        if (friendsConversationPanel != null)
            friendsConversationPanel.SetActive(isFriends && !hasFriendTarget);

        if (friendTools != null)
            friendTools.SetActive(isFriends);

        ApplyFriendChatLayout(isFriends);
        UpdateFriendConversationLabels(hasFriendTarget);

        ApplyInputPlaceholder();
        ApplyTabVisuals();

        if (showMessages)
            RenderActiveHistory();
        else
            ClearVisibleMessages();
    }

    private void ApplyFriendChatLayout(bool isFriends)
    {
        if (chatScrollRectTransform != null)
        {
            chatScrollRectTransform.anchoredPosition = defaultChatScrollPosition;
            chatScrollRectTransform.sizeDelta = defaultChatScrollSize;
        }

        if (chatInputRectTransform != null)
        {
            chatInputRectTransform.anchoredPosition = defaultChatInputPosition;
            chatInputRectTransform.sizeDelta = defaultChatInputSize;
        }

        if (sendButtonRectTransform != null)
            sendButtonRectTransform.anchoredPosition = defaultSendButtonPosition;
    }

    private void UpdateFriendConversationLabels(bool hasFriendTarget)
    {
        string displayName = string.IsNullOrWhiteSpace(friendChatTargetNickname)
            ? ""
            : friendChatTargetNickname;
        bool hasSelection = hasFriendTarget || !string.IsNullOrEmpty(friendChatTargetUserId);

        if (friendSelectorHeaderText != null)
        {
            friendSelectorHeaderText.text = hasSelection
                ? displayName + (hasFriendTarget ? " 대화" : " (오프라인)")
                : "친구 선택";
        }

        if (friendsConversationPanel == null)
            return;

        TMP_Text emptyText = friendsConversationPanel.GetComponentInChildren<TMP_Text>(true);
        if (emptyText == null)
            return;

        if (!string.IsNullOrEmpty(friendChatTargetUserId) && friendChatTargetId == 0)
            emptyText.text = displayName + "님은 현재 오프라인입니다.";
        else
            emptyText.text = "오른쪽 목록에서\n대화할 친구를 선택하세요.";
    }

    private void RenderFriendSelector()
    {
        for (int i = friendSelectorItems.Count - 1; i >= 0; i--)
        {
            FriendListItemUI item = friendSelectorItems[i];
            if (item != null)
            {
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
        }
        friendSelectorItems.Clear();

        bool canRender = friendSelectorContent != null && friendListItemPrefab != null;
        if (friendSelectorEmptyText != null)
        {
            friendSelectorEmptyText.text = friendUsers.Count == 0
                ? "친구가 없습니다."
                : "";
            friendSelectorEmptyText.gameObject.SetActive(friendUsers.Count == 0 || !canRender);
        }

        if (!canRender)
            return;

        for (int i = 0; i < friendUsers.Count; i++)
        {
            FriendUserData friend = friendUsers[i];
            GameObject rowObject = Instantiate(friendListItemPrefab, friendSelectorContent);
            rowObject.name = "FriendChat_" + friend.userId;

            FriendListItemUI row = rowObject.GetComponent<FriendListItemUI>();
            if (row == null)
            {
                Destroy(rowObject);
                continue;
            }

            row.Setup(
                friend,
                FriendListItemMode.ChatSelector,
                friendProfileIcons,
                friendRankIcons,
                SelectFriendConversation
            );
            row.SetChatSelectorSelected(IsSelectedFriend(friend));
            friendSelectorItems.Add(row);
        }
    }

    private void SelectFriendConversation(FriendUserData friend)
    {
        if (friend == null)
            return;

        friendChatTargetId = friend.playerId;
        friendChatTargetUserId = friend.userId ?? "";
        friendChatTargetNickname = GetFriendDisplayName(friend);
        SelectChannel(ChatChannel.Friends);
        RenderFriendSelector();
    }

    private bool IsSelectedFriend(FriendUserData friend)
    {
        if (friend == null)
            return false;

        if (!string.IsNullOrEmpty(friendChatTargetUserId))
            return friend.userId == friendChatTargetUserId;

        return friendChatTargetId != 0 && friend.playerId == friendChatTargetId;
    }

    private static string GetFriendDisplayName(FriendUserData friend)
    {
        if (friend == null)
            return "";

        return string.IsNullOrWhiteSpace(friend.memo)
            ? friend.nickname ?? ""
            : friend.memo;
    }

    private void ApplyInputPlaceholder()
    {
        if (ChatInputField != null && ChatInputField.placeholder is TMP_Text placeholder)
        {
            if (activeChannel == ChatChannel.Room)
                placeholder.text = "방 메시지를 입력하세요.";
            else if (activeChannel == ChatChannel.Friends)
                placeholder.text = string.IsNullOrEmpty(friendChatTargetNickname)
                    ? "친구 메시지를 입력하세요."
                    : friendChatTargetNickname + "에게 메시지를 입력하세요.";
            else
                placeholder.text = "로비 메시지를 입력하세요.";
        }
    }

    private void ApplyTabVisuals()
    {
        SetTabSelected(lobbyTabButton, activeChannel == ChatChannel.Lobby);
        SetTabSelected(roomTabButton, activeChannel == ChatChannel.Room);
        SetTabSelected(friendsTabButton, activeChannel == ChatChannel.Friends);
    }

    private void SetTabSelected(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            Sprite sprite = selected ? tabSelectedSprite : tabNormalSprite;
            if (sprite != null)
                image.sprite = sprite;

            image.color = Color.white;
        }
    }

    private void UpdateDirectTabPositions(bool hasRoom)
    {
        SetTabPosition(lobbyTabButton, -845f);
        SetTabPosition(roomTabButton, -725f);
        SetTabPosition(friendsTabButton, hasRoom ? -605f : -725f);
    }

    private static void SetTabPosition(Button button, float x)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, -3f);
    }

    private void ScrollToLatestMessage()
    {
        if (chatScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }

}
