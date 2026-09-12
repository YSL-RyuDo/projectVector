using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum FriendListItemMode
{
    Friend,
    ChatSelector,
    Blocked,
    AddCandidate,
    OutgoingRequest,
    IncomingRequest
}

public class FriendListItemUI : MonoBehaviour
{
    [SerializeField] private Image profileIcon;
    [SerializeField] private Image rankIcon;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button primaryButton;
    [SerializeField] private TMP_Text primaryButtonText;
    [SerializeField] private Button secondaryButton;
    [SerializeField] private TMP_Text secondaryButtonText;

    [Header("Friend Context Menu")]
    [SerializeField] private Button itemButton;
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private TMP_Text contextNameText;
    [SerializeField] private Button memoButton;
    [SerializeField] private Button messageButton;
    [SerializeField] private Button profileButton;
    [SerializeField] private Button careerButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button blockButton;
    [SerializeField] private Button contextCloseButton;
    [SerializeField] private GameObject memoPopup;
    [SerializeField] private TMP_InputField memoInput;
    [SerializeField] private Button memoConfirmButton;
    [SerializeField] private Button memoCancelButton;
    [SerializeField] private GameObject placeholderPopup;
    [SerializeField] private TMP_Text placeholderTitleText;
    [SerializeField] private TMP_Text placeholderBodyText;
    [SerializeField] private Button placeholderCloseButton;

    private FriendUserData currentUser;
    private Transform contextOverlayRoot;
    private Action<FriendUserData, string> memoAction;
    private Action<FriendUserData> messageAction;
    private Action<FriendUserData> deleteAction;
    private Action<FriendUserData> blockAction;
    private Action<FriendListItemUI, bool> interactionStateAction;

    public bool HasOpenInteraction =>
        (contextMenu != null && contextMenu.activeInHierarchy) ||
        (memoPopup != null && memoPopup.activeInHierarchy) ||
        (placeholderPopup != null && placeholderPopup.activeInHierarchy);

    public void Setup(
        FriendUserData user,
        FriendListItemMode mode,
        Sprite[] profileIcons,
        Sprite[] rankIcons,
        Action<FriendUserData> primaryAction,
        Action<FriendUserData> secondaryAction = null)
    {
        if (user == null)
            return;

        currentUser = user;
        bool compactFriend = mode == FriendListItemMode.Friend;
        bool chatSelector = mode == FriendListItemMode.ChatSelector;
        string displayName = compactFriend && !string.IsNullOrWhiteSpace(user.memo)
            ? user.memo
            : user.nickname;

        if (chatSelector && !string.IsNullOrWhiteSpace(user.memo))
            displayName = user.memo;

        SetText(nicknameText, displayName);
        SetText(idText, "ID  " + user.userId);
        SetText(levelText, "Lv." + user.level);
        SetText(statusText, GetPresenceLabel(user.presenceStatus));

        if (statusText != null)
            statusText.color = GetPresenceColor(user.presenceStatus);

        SetSprite(profileIcon, profileIcons, user.playerIconId);
        SetSprite(rankIcon, rankIcons, user.rankImageId);

        ConfigureButtons(
            user,
            mode,
            primaryAction,
            secondaryAction
        );
        ApplyCompactFriendLayout(compactFriend, chatSelector);

        if (chatSelector)
        {
            DismissContextPanels();
            BindSimpleButton(itemButton, () => primaryAction?.Invoke(user));
        }
    }

    public void ConfigureFriendMenu(
        Transform overlayRoot,
        Action<FriendUserData, string> onMemo,
        Action<FriendUserData> onMessage,
        Action<FriendUserData> onDelete,
        Action<FriendUserData> onBlock,
        Action<FriendListItemUI, bool> onInteractionStateChanged)
    {
        contextOverlayRoot = overlayRoot;
        memoAction = onMemo;
        messageAction = onMessage;
        deleteAction = onDelete;
        blockAction = onBlock;
        interactionStateAction = onInteractionStateChanged;

        DismissContextPanels();
        BindSimpleButton(itemButton, OpenContextMenu);
        BindSimpleButton(contextCloseButton, CloseContextMenu);
        BindSimpleButton(memoButton, OpenMemoPopup);
        BindSimpleButton(messageButton, SendMessage);
        BindSimpleButton(profileButton, () => ShowPlaceholder("\uD504\uB85C\uD544 \uBCF4\uAE30"));
        BindSimpleButton(careerButton, () => ShowPlaceholder("\uCD5C\uADFC \uC804\uC801 \uBCF4\uAE30"));
        BindSimpleButton(deleteButton, DeleteFriend);
        BindSimpleButton(blockButton, BlockFriend);
        BindSimpleButton(memoConfirmButton, ConfirmMemo);
        BindSimpleButton(memoCancelButton, CancelMemo);
        BindSimpleButton(placeholderCloseButton, () => placeholderPopup?.SetActive(false));
    }

    private void ConfigureButtons(
        FriendUserData user,
        FriendListItemMode mode,
        Action<FriendUserData> primaryAction,
        Action<FriendUserData> secondaryAction)
    {
        string primaryLabel = mode switch
        {
            FriendListItemMode.Friend => "\uCC44\uD305",
            FriendListItemMode.Blocked => "\uCC28\uB2E8 \uD574\uC81C",
            FriendListItemMode.OutgoingRequest => "\uC2E0\uCCAD \uCDE8\uC18C",
            FriendListItemMode.IncomingRequest => "\uC218\uB77D",
            _ => "\uCE5C\uAD6C \uCD94\uAC00"
        };

        string secondaryLabel = mode switch
        {
            FriendListItemMode.Friend => "\uCC28\uB2E8",
            FriendListItemMode.IncomingRequest => "\uAC70\uC808",
            _ => ""
        };

        BindButton(primaryButton, primaryButtonText, primaryLabel, primaryAction, user);
        BindButton(
            secondaryButton,
            secondaryButtonText,
            secondaryLabel,
            secondaryAction,
            user
        );
    }

    private void ApplyCompactFriendLayout(bool compact, bool chatSelector)
    {
        if (idText != null)
            idText.gameObject.SetActive(!compact && !chatSelector);
        if (levelText != null)
            levelText.gameObject.SetActive(!compact && !chatSelector);
        if (statusText != null)
            statusText.gameObject.SetActive(!compact || chatSelector);
        if (primaryButton != null && (compact || chatSelector))
            primaryButton.gameObject.SetActive(false);
        if (secondaryButton != null && (compact || chatSelector))
            secondaryButton.gameObject.SetActive(false);
        if (itemButton != null)
            itemButton.interactable = compact || chatSelector;

        if (chatSelector)
            ApplyChatSelectorLayout();
    }

    private void ApplyChatSelectorLayout()
    {
        RectTransform row = transform as RectTransform;
        if (row != null)
            row.sizeDelta = new Vector2(row.sizeDelta.x, 62f);

        LayoutElement layout = GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minHeight = 62f;
            layout.preferredHeight = 62f;
        }

        SetRect(profileIcon, new Vector2(0f, 0.5f), new Vector2(42f, 42f), new Vector2(25f, 0f));
        SetRect(nicknameText, new Vector2(0f, 0.5f), new Vector2(86f, 24f), new Vector2(92f, 11f));
        SetRect(statusText, new Vector2(0f, 0.5f), new Vector2(86f, 20f), new Vector2(92f, -13f));
        SetRect(rankIcon, new Vector2(1f, 0.5f), new Vector2(34f, 34f), new Vector2(-21f, 0f));

        if (nicknameText != null)
        {
            nicknameText.fontSize = 15f;
            nicknameText.alignment = TextAlignmentOptions.Left;
            nicknameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (statusText != null)
        {
            statusText.fontSize = 12f;
            statusText.alignment = TextAlignmentOptions.Left;
        }
    }

    public void SetChatSelectorSelected(bool selected)
    {
        Image background = GetComponent<Image>();
        if (background != null)
        {
            background.color = selected
                ? new Color(0.08f, 0.48f, 0.68f, 0.98f)
                : new Color(0.12f, 0.15f, 0.21f, 0.94f);
        }
    }

    private static void SetRect(Component component, Vector2 anchor, Vector2 size, Vector2 position)
    {
        if (component == null)
            return;

        RectTransform rect = component.transform as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void OpenContextMenu()
    {
        if (contextMenu == null || currentUser == null)
            return;

        if (contextOverlayRoot != null)
            contextMenu.transform.SetParent(contextOverlayRoot, false);

        SetText(
            contextNameText,
            string.IsNullOrWhiteSpace(currentUser.memo)
                ? currentUser.nickname
                : currentUser.memo
        );
        memoPopup?.SetActive(false);
        placeholderPopup?.SetActive(false);
        contextMenu.SetActive(true);
        contextMenu.transform.SetAsLastSibling();
        interactionStateAction?.Invoke(this, true);
    }

    private void CloseContextMenu()
    {
        if (memoPopup != null && memoPopup.activeSelf)
            return;

        placeholderPopup?.SetActive(false);
        contextMenu?.SetActive(false);
        interactionStateAction?.Invoke(this, false);
    }

    public void ForceDismissInteraction()
    {
        memoInput?.DeactivateInputField();
        memoPopup?.SetActive(false);
        placeholderPopup?.SetActive(false);
        contextMenu?.SetActive(false);
        if (contextCloseButton != null)
            contextCloseButton.interactable = true;
        interactionStateAction?.Invoke(this, false);
    }

    private void DismissContextPanels()
    {
        if (contextMenu != null)
            contextMenu.SetActive(false);
        if (memoPopup != null)
            memoPopup.SetActive(false);
        if (placeholderPopup != null)
            placeholderPopup.SetActive(false);
    }

    private void OpenMemoPopup()
    {
        if (memoInput != null)
        {
            memoInput.characterLimit = 8;
            memoInput.SetTextWithoutNotify(currentUser?.memo ?? "");
        }
        if (contextCloseButton != null)
            contextCloseButton.interactable = false;

        memoPopup?.SetActive(true);
        memoPopup?.transform.SetAsLastSibling();
        StartCoroutine(FocusMemoInputNextFrame());
    }

    private IEnumerator FocusMemoInputNextFrame()
    {
        yield return null;

        if (memoInput == null || !memoInput.gameObject.activeInHierarchy)
            yield break;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(memoInput.gameObject);
        memoInput.Select();
        memoInput.ActivateInputField();
    }

    private void CancelMemo()
    {
        CloseMemoPopup();
    }

    private void CloseMemoPopup()
    {
        memoInput?.DeactivateInputField();
        memoPopup?.SetActive(false);
        if (contextCloseButton != null)
            contextCloseButton.interactable = true;
    }

    private void ConfirmMemo()
    {
        if (currentUser == null)
            return;

        string memo = memoInput != null ? memoInput.text.Trim() : "";
        if (memo.Length > 8)
            memo = memo.Substring(0, 8);

        currentUser.memo = memo;
        SetText(nicknameText, string.IsNullOrEmpty(memo) ? currentUser.nickname : memo);
        SetText(contextNameText, string.IsNullOrEmpty(memo) ? currentUser.nickname : memo);
        memoAction?.Invoke(currentUser, memo);
        CloseMemoPopup();
    }

    private void SendMessage()
    {
        CloseContextMenu();
        messageAction?.Invoke(currentUser);
    }

    private void DeleteFriend()
    {
        CloseContextMenu();
        deleteAction?.Invoke(currentUser);
    }

    private void BlockFriend()
    {
        CloseContextMenu();
        blockAction?.Invoke(currentUser);
    }

    private void ShowPlaceholder(string title)
    {
        SetText(placeholderTitleText, title);
        SetText(placeholderBodyText, "\uAE30\uB2A5 \uC5F0\uACB0 \uC900\uBE44 \uC911\uC785\uB2C8\uB2E4.");
        placeholderPopup?.SetActive(true);
    }

    private static void BindSimpleButton(Button button, Action action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (action != null)
            button.onClick.AddListener(() => action());
    }

    private void OnDestroy()
    {
        interactionStateAction?.Invoke(this, false);
        if (contextMenu != null && contextMenu.transform.parent != transform)
            Destroy(contextMenu);
    }

    private static void BindButton(
        Button button,
        TMP_Text label,
        string text,
        Action<FriendUserData> action,
        FriendUserData user)
    {
        if (button == null)
            return;

        bool visible = !string.IsNullOrEmpty(text) && action != null;
        button.gameObject.SetActive(visible);
        button.onClick.RemoveAllListeners();

        if (!visible)
            return;

        SetText(label, text);
        button.onClick.AddListener(() => action(user));
    }

    private static void SetSprite(Image image, Sprite[] sprites, int index)
    {
        if (image == null)
            return;

        if (sprites != null && index >= 0 && index < sprites.Length)
            image.sprite = sprites[index];

        image.enabled = image.sprite != null;
    }

    private static string GetPresenceLabel(FriendPresenceStatus status)
    {
        return status switch
        {
            FriendPresenceStatus.Online => "\uC628\uB77C\uC778",
            FriendPresenceStatus.DoNotDisturb => "\uBC29\uD574 \uAE08\uC9C0",
            _ => "\uC624\uD504\uB77C\uC778"
        };
    }

    private static Color GetPresenceColor(FriendPresenceStatus status)
    {
        return status switch
        {
            FriendPresenceStatus.Online => new Color(0.33f, 0.9f, 0.42f),
            FriendPresenceStatus.DoNotDisturb => new Color(0.95f, 0.32f, 0.32f),
            _ => new Color(0.58f, 0.62f, 0.68f)
        };
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
            label.text = value ?? "";
    }
}
