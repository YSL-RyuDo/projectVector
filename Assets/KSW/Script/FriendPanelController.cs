using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private Button openButton;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button friendAddButton;
    [SerializeField] private GameObject addPopup;
    [SerializeField] private Button addPopupCloseButton;

    [Header("My Profile")]
    [SerializeField] private Image myProfileIcon;
    [SerializeField] private TMP_Text myNicknameText;
    [SerializeField] private TMP_Text myIdText;
    [SerializeField] private TMP_Dropdown presenceDropdown;

    [Header("Main Tabs")]
    [SerializeField] private Button contactsTabButton;
    [SerializeField] private Button recentTabButton;
    [SerializeField] private GameObject contactsView;
    [SerializeField] private GameObject recentView;
    [SerializeField] private Transform friendsContent;
    [SerializeField] private Transform blockedContent;
    [SerializeField] private Transform recentContent;
    [SerializeField] private TMP_Text friendsEmptyText;
    [SerializeField] private TMP_Text blockedEmptyText;
    [SerializeField] private TMP_Text recentEmptyText;

    [Header("Add Popup")]
    [SerializeField] private Button searchTabButton;
    [SerializeField] private Button requestTabButton;
    [SerializeField] private GameObject searchView;
    [SerializeField] private GameObject requestView;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button searchButton;
    [SerializeField] private Button recommendationRefreshButton;
    [SerializeField] private Transform searchResultContent;
    [SerializeField] private Transform recommendationContent;
    [SerializeField] private Transform outgoingRequestContent;
    [SerializeField] private Transform incomingRequestContent;
    [SerializeField] private TMP_Text searchEmptyText;
    [SerializeField] private TMP_Text recommendationEmptyText;
    [SerializeField] private TMP_Text outgoingEmptyText;
    [SerializeField] private TMP_Text incomingEmptyText;
    [SerializeField] private TMP_Text operationMessageText;

    [Header("List Assets")]
    [SerializeField] private GameObject friendListItemPrefab;
    [SerializeField] private Sprite[] profileIcons;
    [SerializeField] private Sprite[] rankIcons;

    [Header("Chat")]
    [SerializeField] private ChatPanelController chatPanelController;

    private readonly List<FriendUserData> friends = new();
    private readonly List<FriendUserData> blocked = new();
    private readonly List<FriendUserData> recent = new();
    private readonly List<FriendUserData> searchResults = new();
    private readonly List<FriendUserData> recommendations = new();
    private readonly List<FriendRequestData> requests = new();

    private Coroutine presenceRefreshCoroutine;
    private FriendListItemUI activeInteractionItem;

    private void Start()
    {
        PrepareListViewports();
        BindControls();
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SetFriendPanelController(this);

        panelRoot?.SetActive(false);
        addPopup?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.ClearFriendPanelController(this);
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
            return;

        if (panelRoot.activeSelf)
            ClosePanel();
        else
            OpenPanel();
    }

    public void OpenPanel()
    {
        panelRoot?.SetActive(true);
        addPopup?.SetActive(false);
        ShowContacts();
        RefreshMyProfile();
        RefreshAllFromServer();

        if (presenceRefreshCoroutine != null)
            StopCoroutine(presenceRefreshCoroutine);
        presenceRefreshCoroutine = StartCoroutine(RefreshPresenceLoop());
    }

    public void ClosePanel()
    {
        activeInteractionItem?.ForceDismissInteraction();
        activeInteractionItem = null;
        addPopup?.SetActive(false);
        panelRoot?.SetActive(false);
        if (presenceRefreshCoroutine != null)
        {
            StopCoroutine(presenceRefreshCoroutine);
            presenceRefreshCoroutine = null;
        }
    }

    public void ApplyOverview(
        FriendPresenceMode presenceMode,
        List<FriendUserData> friendUsers,
        List<FriendUserData> blockedUsers)
    {
        friends.Clear();
        blocked.Clear();
        if (friendUsers != null)
            friends.AddRange(friendUsers);
        if (blockedUsers != null)
            blocked.AddRange(blockedUsers);

        if (presenceDropdown != null)
            presenceDropdown.SetValueWithoutNotify((int)presenceMode);

        if (!HasOpenFriendInteraction())
            RenderContacts();
    }

    public void ApplyRecent(List<FriendUserData> users)
    {
        recent.Clear();
        if (users != null)
            recent.AddRange(users);
        RenderRecent();
    }

    public void ApplySearchResults(List<FriendUserData> users)
    {
        searchResults.Clear();
        if (users != null)
            searchResults.AddRange(users);
        RenderUsers(
            searchResultContent,
            searchResults,
            FriendListItemMode.AddCandidate,
            SendFriendRequest
        );
        SetEmpty(searchEmptyText, searchResults.Count == 0);
        SetOperationMessage("");
    }

    public void ApplyRecommendations(List<FriendUserData> users)
    {
        recommendations.Clear();
        if (users != null)
            recommendations.AddRange(users);
        RenderUsers(
            recommendationContent,
            recommendations,
            FriendListItemMode.AddCandidate,
            SendFriendRequest
        );
        SetEmpty(recommendationEmptyText, recommendations.Count == 0);
    }

    public void ApplyRequests(List<FriendRequestData> values)
    {
        requests.Clear();
        if (values != null)
            requests.AddRange(values);

        ClearContent(outgoingRequestContent);
        ClearContent(incomingRequestContent);
        int outgoingCount = 0;
        int incomingCount = 0;

        foreach (FriendRequestData request in requests)
        {
            if (request == null || request.user == null)
                continue;

            if (request.incoming)
            {
                CreateItem(
                    incomingRequestContent,
                    request.user,
                    FriendListItemMode.IncomingRequest,
                    user => SendOperation(FriendOperation.AcceptRequest, user),
                    user => SendOperation(FriendOperation.RejectRequest, user)
                );
                incomingCount++;
            }
            else
            {
                CreateItem(
                    outgoingRequestContent,
                    request.user,
                    FriendListItemMode.OutgoingRequest,
                    user => SendOperation(FriendOperation.CancelRequest, user)
                );
                outgoingCount++;
            }
        }

        SetEmpty(outgoingEmptyText, outgoingCount == 0);
        SetEmpty(incomingEmptyText, incomingCount == 0);
    }

    public void ApplyPresenceResult(bool success, FriendPresenceMode mode)
    {
        SetOperationMessage(success ? "\uC0C1\uD0DC\uAC00 \uBCC0\uACBD\uB418\uC5C8\uC2B5\uB2C8\uB2E4." : "\uC0C1\uD0DC \uBCC0\uACBD\uC5D0 \uC2E4\uD328\uD588\uC2B5\uB2C8\uB2E4.");
        if (presenceDropdown != null)
            presenceDropdown.SetValueWithoutNotify((int)mode);
    }

    public void ApplyOperationResult(
        FriendOperation operation,
        FriendOperationResult result,
        string targetUserId)
    {
        SetOperationMessage(GetOperationMessage(operation, result, targetUserId));
        RefreshAllFromServer();
        if (addPopup != null && addPopup.activeSelf)
        {
            NetworkManager.Instance?.SendFriendRequestListRequest();
            NetworkManager.Instance?.SendFriendRecommendRequest();
            SearchCurrentText();
        }
    }

    private void BindControls()
    {
        Bind(openButton, TogglePanel);
        Bind(closeButton, ClosePanel);
        Bind(friendAddButton, OpenAddPopup);
        Bind(addPopupCloseButton, () => addPopup?.SetActive(false));
        Bind(contactsTabButton, ShowContacts);
        Bind(recentTabButton, ShowRecent);
        Bind(searchTabButton, ShowSearch);
        Bind(requestTabButton, ShowRequests);
        Bind(searchButton, SearchCurrentText);
        Bind(recommendationRefreshButton, () =>
            NetworkManager.Instance?.SendFriendRecommendRequest());

        if (searchInput != null)
        {
            searchInput.onSubmit.RemoveAllListeners();
            searchInput.onSubmit.AddListener(_ => SearchCurrentText());
        }

        if (presenceDropdown != null)
        {
            presenceDropdown.ClearOptions();
            presenceDropdown.AddOptions(new List<string> { "\uC628\uB77C\uC778", "\uBC29\uD574 \uAE08\uC9C0" });
            presenceDropdown.onValueChanged.RemoveAllListeners();
            presenceDropdown.onValueChanged.AddListener(value =>
                NetworkManager.Instance?.SendFriendPresenceSet(
                    value == 1
                        ? FriendPresenceMode.DoNotDisturb
                        : FriendPresenceMode.Online));
        }
    }

    private void RefreshMyProfile()
    {
        ProfilePanelController profile = FindFirstObjectByType<ProfilePanelController>();
        if (profile != null)
        {
            SetText(myNicknameText, profile.currentNickname);
            SetProfileSprite(myProfileIcon, profile.currentplayerIconId);
        }

        SetText(
            myIdText,
            "ID  " + (NetworkManager.Instance != null
                ? NetworkManager.Instance.MyUserId
                : "")
        );
    }

    private void RefreshAllFromServer()
    {
        NetworkManager.Instance?.SendFriendOverviewRequest();
        NetworkManager.Instance?.SendFriendRecentRequest();
    }

    private IEnumerator RefreshPresenceLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(5f);
        while (panelRoot != null && panelRoot.activeSelf)
        {
            yield return wait;
            if (!HasOpenFriendInteraction())
                NetworkManager.Instance?.SendFriendOverviewRequest();
        }
        presenceRefreshCoroutine = null;
    }

    private void OpenAddPopup()
    {
        addPopup?.SetActive(true);
        ShowSearch();
        SetOperationMessage("");
    }

    private void ShowContacts()
    {
        contactsView?.SetActive(true);
        recentView?.SetActive(false);
        ApplyTabVisuals(contactsTabButton, recentTabButton);
        RenderContacts();
    }

    private void ShowRecent()
    {
        contactsView?.SetActive(false);
        recentView?.SetActive(true);
        ApplyTabVisuals(recentTabButton, contactsTabButton);
        NetworkManager.Instance?.SendFriendRecentRequest();
        RenderRecent();
    }

    private void ShowSearch()
    {
        searchView?.SetActive(true);
        requestView?.SetActive(false);
        ApplyTabVisuals(searchTabButton, requestTabButton);
        NetworkManager.Instance?.SendFriendRecommendRequest();
    }

    private void ShowRequests()
    {
        searchView?.SetActive(false);
        requestView?.SetActive(true);
        ApplyTabVisuals(requestTabButton, searchTabButton);
        NetworkManager.Instance?.SendFriendRequestListRequest();
    }

    private void SearchCurrentText()
    {
        string query = searchInput != null ? searchInput.text.Trim() : "";
        if (query.Length == 0)
        {
            searchResults.Clear();
            RenderUsers(
                searchResultContent,
                searchResults,
                FriendListItemMode.AddCandidate,
                SendFriendRequest
            );
            SetEmpty(searchEmptyText, true);
            return;
        }

        NetworkManager network = NetworkManager.Instance;
        if (network == null)
        {
            SetOperationMessage("NetworkManager\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.");
            Debug.LogWarning("[FRIEND SEARCH] NetworkManager is missing");
            return;
        }

        if (!network.IsConnected)
        {
            SetOperationMessage("\uC11C\uBC84\uC5D0 \uC5F0\uACB0\uB418\uC5B4 \uC788\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4.");
            Debug.LogWarning("[FRIEND SEARCH] server is not connected");
            return;
        }

        if (!network.IsAuthenticated)
        {
            SetOperationMessage("\uB85C\uADF8\uC778 \uC644\uB8CC \uD6C4 \uAC80\uC0C9\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.");
            Debug.LogWarning("[FRIEND SEARCH] login is not complete");
            return;
        }

        network.SendFriendSearch(query);
    }

    private bool HasOpenFriendInteraction()
    {
        return activeInteractionItem != null;
    }

    private void SetFriendInteractionState(
        FriendListItemUI item,
        bool active)
    {
        if (active)
        {
            activeInteractionItem = item;
            return;
        }

        if (activeInteractionItem == item)
            activeInteractionItem = null;
    }

    private void RenderContacts()
    {
        RenderUsers(
            friendsContent,
            friends,
            FriendListItemMode.Friend,
            null
        );
        RenderUsers(
            blockedContent,
            blocked,
            FriendListItemMode.Blocked,
            user => SendOperation(FriendOperation.Unblock, user)
        );
        SetEmpty(friendsEmptyText, friends.Count == 0);
        SetEmpty(blockedEmptyText, blocked.Count == 0);
    }

    private void RenderRecent()
    {
        RenderUsers(
            recentContent,
            recent,
            FriendListItemMode.AddCandidate,
            SendFriendRequest
        );
        SetEmpty(recentEmptyText, recent.Count == 0);
    }

    private void RenderUsers(
        Transform content,
        List<FriendUserData> users,
        FriendListItemMode mode,
        System.Action<FriendUserData> primary,
        System.Action<FriendUserData> secondary = null)
    {
        ClearContent(content);
        foreach (FriendUserData user in users)
            CreateItem(content, user, mode, primary, secondary);

        if (content is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void PrepareListViewports()
    {
        PrepareListViewport(friendsContent);
        PrepareListViewport(blockedContent);
        PrepareListViewport(recentContent);
        PrepareListViewport(searchResultContent);
        PrepareListViewport(recommendationContent);
        PrepareListViewport(outgoingRequestContent);
        PrepareListViewport(incomingRequestContent);
    }

    private static void PrepareListViewport(Transform content)
    {
        if (content == null || content.parent == null)
            return;

        Mask mask = content.parent.GetComponent<Mask>();
        Image maskImage = content.parent.GetComponent<Image>();
        if (mask == null || maskImage == null)
            return;

        Color color = maskImage.color;
        color.a = 1f;
        maskImage.color = color;
        mask.showMaskGraphic = false;
    }

    private static void ApplyTabVisuals(Button selected, Button unselected)
    {
        SetTabVisual(selected, true);
        SetTabVisual(unselected, false);
    }

    private static void SetTabVisual(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected
                ? new Color32(203, 43, 58, 255)
                : new Color32(48, 53, 64, 255);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.color = Color.white;
    }

    private void CreateItem(
        Transform content,
        FriendUserData user,
        FriendListItemMode mode,
        System.Action<FriendUserData> primary,
        System.Action<FriendUserData> secondary = null)
    {
        if (content == null || friendListItemPrefab == null || user == null)
            return;

        GameObject item = Instantiate(friendListItemPrefab, content);
        item.SetActive(true);
        FriendListItemUI itemUi = item.GetComponent<FriendListItemUI>();
        itemUi?.Setup(
            user,
            mode,
            profileIcons,
            rankIcons,
            primary,
            secondary
        );

        if (mode == FriendListItemMode.Friend && itemUi != null)
        {
            itemUi.ConfigureFriendMenu(
                panelRoot != null ? panelRoot.transform : transform,
                (selected, memo) =>
                    NetworkManager.Instance?.SendFriendMemoSet(selected.userId, memo),
                OpenFriendChat,
                selected => SendOperation(FriendOperation.DeleteFriend, selected),
                selected => SendOperation(FriendOperation.Block, selected),
                SetFriendInteractionState
            );
        }
    }

    private void OpenFriendChat(FriendUserData user)
    {
        if (user == null || user.playerId == 0)
        {
            SetOperationMessage("\uC624\uD504\uB77C\uC778 \uCE5C\uAD6C\uC5D0\uAC8C\uB294 \uD604\uC7AC \uBA54\uC2DC\uC9C0\uB97C \uBCF4\uB0BC \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
            return;
        }

        chatPanelController?.OpenFriendChat(
            user.playerId,
            string.IsNullOrWhiteSpace(user.memo) ? user.nickname : user.memo
        );
        ClosePanel();
    }

    private void SendFriendRequest(FriendUserData user)
    {
        SendOperation(FriendOperation.SendRequest, user);
    }

    private void SendOperation(FriendOperation operation, FriendUserData user)
    {
        if (user == null)
            return;
        NetworkManager.Instance?.SendFriendOperation(operation, user.userId);
    }

    private void SetProfileSprite(Image image, int index)
    {
        if (image == null)
            return;
        if (profileIcons != null && index >= 0 && index < profileIcons.Length)
            image.sprite = profileIcons[index];
        image.enabled = image.sprite != null;
    }

    private static void ClearContent(Transform content)
    {
        if (content == null)
            return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    private static void SetEmpty(TMP_Text label, bool visible)
    {
        if (label != null)
            label.gameObject.SetActive(visible);
    }

    private void SetOperationMessage(string value)
    {
        SetText(operationMessageText, value);
    }

    private static string GetOperationMessage(
        FriendOperation operation,
        FriendOperationResult result,
        string targetUserId)
    {
        if (result == FriendOperationResult.Success)
        {
            return operation switch
            {
                FriendOperation.SendRequest => "\uCE5C\uAD6C \uC2E0\uCCAD\uC744 \uBCF4\uB0C8\uC2B5\uB2C8\uB2E4.",
                FriendOperation.CancelRequest => "\uCE5C\uAD6C \uC2E0\uCCAD\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.",
                FriendOperation.AcceptRequest => "\uCE5C\uAD6C \uC2E0\uCCAD\uC744 \uC218\uB77D\uD588\uC2B5\uB2C8\uB2E4.",
                FriendOperation.RejectRequest => "\uCE5C\uAD6C \uC2E0\uCCAD\uC744 \uAC70\uC808\uD588\uC2B5\uB2C8\uB2E4.",
                FriendOperation.Block => "\uCC28\uB2E8\uD588\uC2B5\uB2C8\uB2E4.",
                FriendOperation.Unblock => "\uCC28\uB2E8\uC744 \uD574\uC81C\uD588\uC2B5\uB2C8\uB2E4.",
                FriendOperation.DeleteFriend => "\uCE5C\uAD6C\uB97C \uC0AD\uC81C\uD588\uC2B5\uB2C8\uB2E4.",
                _ => "\uCC98\uB9AC\uB418\uC5C8\uC2B5\uB2C8\uB2E4."
            };
        }

        return result switch
        {
            FriendOperationResult.Self => "\uC790\uAE30 \uC790\uC2E0\uC740 \uC120\uD0DD\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.",
            FriendOperationResult.AlreadyFriend => "\uC774\uBBF8 \uCE5C\uAD6C\uC785\uB2C8\uB2E4.",
            FriendOperationResult.AlreadyPending => "\uC774\uBBF8 \uCC98\uB9AC \uB300\uAE30 \uC911\uC778 \uC2E0\uCCAD\uC785\uB2C8\uB2E4.",
            FriendOperationResult.Blocked => "\uCC28\uB2E8 \uAD00\uACC4\uC5D0\uC11C\uB294 \uCC98\uB9AC\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.",
            FriendOperationResult.NotPending => "\uD574\uB2F9 \uC2E0\uCCAD\uC774 \uC874\uC7AC\uD558\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4.",
            FriendOperationResult.NotFriend => "\uCE5C\uAD6C \uAD00\uACC4\uAC00 \uC874\uC7AC\uD558\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4.",
            _ => string.IsNullOrEmpty(targetUserId)
                ? "\uC694\uCCAD \uCC98\uB9AC\uC5D0 \uC2E4\uD328\uD588\uC2B5\uB2C8\uB2E4."
                : targetUserId + " \uC694\uCCAD \uCC98\uB9AC\uC5D0 \uC2E4\uD328\uD588\uC2B5\uB2C8\uB2E4."
        };
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
            label.text = value ?? "";
    }
}
