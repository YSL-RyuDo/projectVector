using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum InGameChatChannel : byte
{
    Team = 0,
    Match = 1
}

public class InGameChatUI : MonoBehaviour
{
    [Header("Message Visibility")]
    [SerializeField] private CanvasGroup messageCanvasGroup;
    [SerializeField] private float messageVisibleSeconds = 4f;
    [SerializeField] private float messageFadeDuration = 0.25f;

    private Coroutine messageVisibilityCoroutine;

    public static InGameChatUI Instance { get; private set; }

    public static bool IsChatOpen
    {
        get
        {
            return Instance != null &&
                   Instance.isChatOpen;
        }
    }

    private static int tabConsumedFrame = -1;

    public static bool WasTabConsumedThisFrame
    {
        get
        {
            return tabConsumedFrame ==
                   Time.frameCount;
        }
    }

    [Header("Roots")]
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private GameObject inputRoot;

    [Header("Input")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button channelButton;
    [SerializeField] private TMP_Text channelText;

    [Header("Message List")]
    [SerializeField] private Transform messageContent;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Settings")]
    [SerializeField] private int maxMessages = 50;
    [SerializeField] private int inputCharacterLimit = 60;

    private readonly List<GameObject> spawnedMessages =
        new List<GameObject>();

    private InGameChatChannel currentChannel =
        InGameChatChannel.Team;

    private bool isChatOpen;
    private int openedFrame = -1;

    private Coroutine focusCoroutine;
    private Coroutine scrollCoroutine;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (inputField != null)
        {
            inputField.lineType =
                TMP_InputField.LineType.SingleLine;

            inputField.characterLimit =
                inputCharacterLimit;

            inputField.richText = false;
        }

        if (channelButton != null)
        {
            channelButton.onClick.RemoveListener(
                ToggleChannel
            );

            channelButton.onClick.AddListener(
                ToggleChannel
            );
        }

        if (messageRoot != null)
            messageRoot.SetActive(true);

        if (inputRoot != null)
            inputRoot.SetActive(false);

        isChatOpen = false;

        RefreshChannelText();

        if (messageRoot != null)
        {
            if (messageCanvasGroup == null)
            {
                messageCanvasGroup =
                    messageRoot.GetComponent<CanvasGroup>();

                if (messageCanvasGroup == null)
                {
                    messageCanvasGroup =
                        messageRoot.AddComponent<CanvasGroup>();
                }
            }

            messageRoot.SetActive(true);

            messageCanvasGroup.alpha = 0f;
            messageCanvasGroup.interactable = false;
            messageCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        if (channelButton != null)
        {
            channelButton.onClick.RemoveListener(
                ToggleChannel
            );
        }

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        bool enterPressed =
            keyboard.enterKey.wasPressedThisFrame ||
            keyboard.numpadEnterKey.wasPressedThisFrame;

        if (!isChatOpen)
        {
            if (enterPressed)
            {
                if (GameController.Instance != null &&
                    GameController.Instance.IsPaused)
                {
                    return;
                }

                OpenChat();
            }

            return;
        }

        if (keyboard.tabKey.wasPressedThisFrame)
        {
            tabConsumedFrame = Time.frameCount;
            ToggleChannel();
            return;
        }

        if (enterPressed &&
            openedFrame != Time.frameCount)
        {
            SubmitAndClose();
        }
    }

    public void OpenChat()
    {
        if (isChatOpen)
            return;

        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
        {
            return;
        }

        isChatOpen = true;
        openedFrame = Time.frameCount;

        // 입력 중에는 채팅 목록이 계속 보인다.
        KeepMessagesVisible();

        if (InGameScoreboardUI.Instance != null)
        {
            InGameScoreboardUI.Instance.Close();
        }

        ForceExitLocalADS();

        if (inputRoot != null)
        {
            inputRoot.SetActive(true);
            inputRoot.transform.SetAsLastSibling();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartFocusCoroutine(true);

        Debug.Log($"[IN GAME CHAT OPEN] channel={currentChannel}");
    }

    public void CloseChat()
    {
        if (!isChatOpen)
            return;

        isChatOpen = false;

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }

        if (inputField != null)
        {
            inputField.DeactivateInputField();
            inputField.text = string.Empty;
        }

        if (inputRoot != null)
            inputRoot.SetActive(false);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        bool gamePaused =
            GameController.Instance != null &&
            GameController.Instance.IsPaused;

        if (!gamePaused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 채팅을 닫아도 바로 없어지지 않고
        // 몇 초간 보인 뒤 사라진다.
        StartMessageHideTimer();

        Debug.Log("[IN GAME CHAT CLOSE]");
    }

    private void SubmitAndClose()
    {
        string message =
            inputField != null
                ? inputField.text.Trim()
                : string.Empty;

        if (!string.IsNullOrWhiteSpace(message))
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendInGameChat(
                    currentChannel,
                    message
                );
            }
            else
            {
                Debug.LogError(
                    "[IN GAME CHAT] NetworkManager.Instance is null"
                );
            }
        }

        CloseChat();
    }

    public void ToggleChannel()
    {
        currentChannel =
            currentChannel ==
            InGameChatChannel.Team
                ? InGameChatChannel.Match
                : InGameChatChannel.Team;

        RefreshChannelText();

        if (isChatOpen)
        {
            StartFocusCoroutine(
                false
            );
        }

        Debug.Log(
            $"[IN GAME CHAT CHANNEL] {currentChannel}"
        );
    }

    public void SetTeamChannel()
    {
        currentChannel =
            InGameChatChannel.Team;

        RefreshChannelText();

        if (isChatOpen)
        {
            StartFocusCoroutine(
                false
            );
        }
    }

    public void SetMatchChannel()
    {
        currentChannel =
            InGameChatChannel.Match;

        RefreshChannelText();

        if (isChatOpen)
        {
            StartFocusCoroutine(
                false
            );
        }
    }

    private void RefreshChannelText()
    {
        if (channelText == null)
            return;

        channelText.text =
            currentChannel ==
            InGameChatChannel.Team
                ? "팀"
                : "매치";
    }

    private void StartFocusCoroutine(
        bool clearText)
    {
        if (focusCoroutine != null)
        {
            StopCoroutine(
                focusCoroutine
            );
        }

        focusCoroutine =
            StartCoroutine(
                FocusInputNextFrame(
                    clearText
                )
            );
    }

    private IEnumerator FocusInputNextFrame(
        bool clearText)
    {
        yield return null;

        if (!isChatOpen ||
            inputField == null)
        {
            focusCoroutine = null;
            yield break;
        }

        if (clearText)
        {
            inputField.text =
                string.Empty;
        }

        inputField.Select();
        inputField.ActivateInputField();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                inputField.gameObject
            );
        }

        inputField.MoveTextEnd(false);

        focusCoroutine = null;
    }

    public void OnReceiveChat(
        uint senderPlayerId,
        InGameChatChannel channel,
        byte senderTeam,
        string nickname,
        string message)
    {
        if (messagePrefab == null)
        {
            Debug.LogError(
                "[IN GAME CHAT] messagePrefab is null"
            );

            return;
        }

        if (messageContent == null)
        {
            Debug.LogError(
                "[IN GAME CHAT] messageContent is null"
            );

            return;
        }

        GameObject messageObject =
            Instantiate(
                messagePrefab,
                messageContent
            );

        TMP_Text messageText =
            messageObject.GetComponent<TMP_Text>();

        if (messageText == null)
        {
            messageText =
                messageObject.GetComponentInChildren<TMP_Text>(
                    true
                );
        }

        string safeNickname =
            EscapeRichText(
                nickname
            );

        string safeMessage =
            EscapeRichText(
                message
            );

        if (messageText != null)
        {
            if (channel ==
                InGameChatChannel.Team)
            {
                messageText.text =
                    $"<color=#62C8FF>[팀]</color> " +
                    $"{safeNickname}: {safeMessage}";
            }
            else
            {
                messageText.text =
                    $"<color=#FFD166>[매치]</color> " +
                    $"{safeNickname}: {safeMessage}";
            }
        }

        spawnedMessages.Add(
            messageObject
        );

        while (spawnedMessages.Count >
               Mathf.Max(1, maxMessages))
        {
            GameObject oldest =
                spawnedMessages[0];

            spawnedMessages.RemoveAt(0);

            if (oldest != null)
                Destroy(oldest);
        }

        ScrollToBottom();

        if (isChatOpen)
        {
            // 채팅 입력 중에는 계속 표시
            KeepMessagesVisible();
        }
        else
        {
            // 새 메시지가 오면 몇 초 표시 후 숨김
            ShowMessagesTemporarily();
        }

        Debug.Log(
            $"[IN GAME CHAT RECEIVE] " +
            $"sender={senderPlayerId}, " +
            $"team={senderTeam}, " +
            $"channel={channel}, " +
            $"nickname={nickname}, " +
            $"message={message}"
        );
    }

    private string EscapeRichText(
        string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null)
            return;

        if (scrollCoroutine != null)
        {
            StopCoroutine(
                scrollCoroutine
            );
        }

        scrollCoroutine =
            StartCoroutine(
                ScrollToBottomNextFrame()
            );
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (messageContent != null)
        {
            RectTransform contentRect =
                messageContent as RectTransform;

            if (contentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    contentRect
                );
            }
        }

        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        scrollCoroutine = null;
    }

    private void ForceExitLocalADS()
    {
        FPSCameraLook[] cameraLooks =
            FindObjectsByType<FPSCameraLook>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0;
             i < cameraLooks.Length;
             i++)
        {
            FPSCameraLook look =
                cameraLooks[i];

            if (look == null ||
                !look.enabled)
            {
                continue;
            }

            look.ForceExitADS();
            break;
        }
    }

    private void ShowMessageRoot(bool allowInteraction)
    {
        if (messageRoot == null)
            return;

        messageRoot.SetActive(true);

        if (messageCanvasGroup == null)
            return;

        messageCanvasGroup.alpha = 1f;
        messageCanvasGroup.interactable = allowInteraction;
        messageCanvasGroup.blocksRaycasts = allowInteraction;
    }

    private void KeepMessagesVisible()
    {
        if (messageVisibilityCoroutine != null)
        {
            StopCoroutine(messageVisibilityCoroutine);
            messageVisibilityCoroutine = null;
        }

        ShowMessageRoot(true);
    }

    private void ShowMessagesTemporarily()
    {
        if (messageVisibilityCoroutine != null)
        {
            StopCoroutine(messageVisibilityCoroutine);
            messageVisibilityCoroutine = null;
        }

        ShowMessageRoot(false);

        if (!isChatOpen)
        {
            messageVisibilityCoroutine =
                StartCoroutine(HideMessagesAfterDelay());
        }
    }

    private void StartMessageHideTimer()
    {
        if (messageVisibilityCoroutine != null)
        {
            StopCoroutine(messageVisibilityCoroutine);
            messageVisibilityCoroutine = null;
        }

        if (isChatOpen)
        {
            KeepMessagesVisible();
            return;
        }

        ShowMessageRoot(false);

        messageVisibilityCoroutine =
            StartCoroutine(HideMessagesAfterDelay());
    }

    private IEnumerator HideMessagesAfterDelay()
    {
        yield return new WaitForSecondsRealtime(
            messageVisibleSeconds
        );

        if (isChatOpen)
        {
            messageVisibilityCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        float startAlpha =
            messageCanvasGroup != null
                ? messageCanvasGroup.alpha
                : 1f;

        while (elapsed < messageFadeDuration)
        {
            if (isChatOpen)
            {
                ShowMessageRoot(true);
                messageVisibilityCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            float t =
                messageFadeDuration <= 0f
                    ? 1f
                    : elapsed / messageFadeDuration;

            if (messageCanvasGroup != null)
            {
                messageCanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        t
                    );
            }

            yield return null;
        }

        if (messageCanvasGroup != null)
        {
            messageCanvasGroup.alpha = 0f;
            messageCanvasGroup.interactable = false;
            messageCanvasGroup.blocksRaycasts = false;
        }

        messageVisibilityCoroutine = null;
    }

    public static bool BlocksGameplayInput
    {
        get
        {
            if (Instance == null)
                return false;

            if (Instance.isChatOpen)
                return true;

            // 상태값이 꼬이더라도 InputField가 실제 포커스 중이면 차단
            if (Instance.inputField != null &&
                Instance.inputField.isFocused)
            {
                return true;
            }

            return false;
        }
    }

    public static bool CloseChatIfOpen()
    {
        if (Instance == null)
            return false;

        if (!Instance.isChatOpen)
            return false;

        Instance.CloseChat();
        return true;
    }

    public static bool TryCloseChatByEscape()
    {
        if (Instance == null)
            return false;

        if (!Instance.isChatOpen)
            return false;

        Instance.CloseChat();
        return true;
    }
}