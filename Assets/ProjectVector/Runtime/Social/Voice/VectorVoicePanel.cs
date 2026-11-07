using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// All referenced objects are authored in scene. Runtime only binds state/input.
public sealed class VectorVoicePanel : MonoBehaviour
{
    public GameObject panel;
    public Button openButton, closeButton, modeButton, microphoneButton, inputDeviceButton, outputDeviceButton;
    public TMP_Text statusText, modeText, microphoneText, inputDeviceText, outputDeviceText, speakingText;
    public Slider inputVolume, outputVolume;
    public TMP_Text[] participantTexts = new TMP_Text[3];
    public Button[] participantMuteButtons = new Button[3];
    public Slider[] participantVolumes = new Slider[3];
    public Button[] participantResetButtons = new Button[3];
    public TMP_Text[] participantStates = new TMP_Text[3], participantVolumeLabels = new TMP_Text[3];
    public Image[] participantIcons = new Image[3], participantActivity = new Image[3];
    public GameObject[] participantControls = new GameObject[3];
    public TMP_Text participantCount;
    public Button partyButton;
    public GameObject partyView;
    public GameObject privateRoomView;
    public GameObject[] hudRows = System.Array.Empty<GameObject>();
    public TMP_Text[] hudNames = System.Array.Empty<TMP_Text>();
    public Image[] hudIcons = System.Array.Empty<Image>(), hudActivity = System.Array.Empty<Image>();
    public GameObject[] blockingPopups = System.Array.Empty<GameObject>();
    private string[] accounts;
    private static readonly HashSet<VectorVoicePanel> activePanels = new();
    private float nextRefresh;
    private CursorLockMode previousLock;
    private bool previousCursor;
    private static int closedFrame = -1;
    public static bool IsAnyOpen => activePanels.Any(p => p != null && p.panel != null && p.panel.activeInHierarchy);
    public static bool BlocksGameplayInput => IsAnyOpen || Time.frameCount <= closedFrame;
    public static bool HasBlockingPopup => activePanels.Any(p => p != null && p.blockingPopups.Any(g => g != null && g.activeInHierarchy));

    private void Awake()
    {
        accounts = new string[participantTexts.Length];
        openButton.onClick.AddListener(Toggle);
        if (partyButton) partyButton.onClick.AddListener(Toggle);
        closeButton.onClick.AddListener(Close);
        modeButton.onClick.AddListener(() => VectorVoiceChat.Instance?.ToggleMode());
        microphoneButton.onClick.AddListener(() => VectorVoiceChat.Instance?.ToggleMicrophone());
        inputDeviceButton.onClick.AddListener(() => VectorVoiceChat.Instance?.CycleDevice(true));
        outputDeviceButton.onClick.AddListener(() => VectorVoiceChat.Instance?.CycleDevice(false));
        inputVolume.onValueChanged.AddListener(v => VectorVoiceChat.Instance?.SetInputVolume(v));
        outputVolume.onValueChanged.AddListener(v => VectorVoiceChat.Instance?.SetOutputVolume(v));
        for (int i = 0; i < participantMuteButtons.Length; ++i)
        {
            int slot = i;
            participantMuteButtons[i].onClick.AddListener(() => { if (!string.IsNullOrEmpty(accounts[slot])) VectorVoiceChat.Instance?.ToggleParticipant(accounts[slot]); });
            participantVolumes[i].onValueChanged.AddListener(v => { if (!string.IsNullOrEmpty(accounts[slot])) VectorVoiceChat.Instance?.SetParticipantVolume(accounts[slot], v); });
            participantResetButtons[i].onClick.AddListener(() => { if (!string.IsNullOrEmpty(accounts[slot])) VectorVoiceChat.Instance?.ResetParticipant(accounts[slot]); });
        }
    }
    private void OnEnable() { activePanels.Add(this); RefreshVisibility(); }
    private void OnDisable() { Close(); activePanels.Remove(this); }
    private void Update()
    {
        if (!RefreshVisibility()) return;
        if (Keyboard.current?.f10Key.wasPressedThisFrame == true) Toggle();
        if (panel.activeSelf && Keyboard.current?.escapeKey.wasPressedThisFrame == true) Close();
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .1f;
        var service = VectorVoiceChat.Instance;
        if (service == null) { statusText.text = "게임 로그인 후 이용할 수 있습니다"; speakingText.text = ""; return; }
        statusText.text = service.Status;
        modeText.text = service.Automatic ? "자동 음성 감지" : "눌러서 말하기  [ ~ ]";
        microphoneText.text = service.MicrophoneMuted ? "마이크 꺼짐" : "마이크 켜짐";
        inputDeviceText.text = service.InputDevice;
        outputDeviceText.text = service.OutputDevice;
        inputVolume.SetValueWithoutNotify(service.InputVolume);
        outputVolume.SetValueWithoutNotify(service.OutputVolume);
        inputDeviceButton.interactable = outputDeviceButton.interactable = service.Ready;
        var members = service.Participants.OrderByDescending(p => p.IsSelf).ThenBy(p => p.PlayerId).Take(accounts.Length).ToArray();
        // A departure/reorder must not redirect an in-flight drag or button release to another teammate.
        bool pointerBusy = Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasReleasedThisFrame);
        if (pointerBusy && Enumerable.Range(0, accounts.Length).Any(i => accounts[i] != (i < members.Length ? members[i].PlayerId : ""))) return;
        participantCount.text = $"{members.Length} / {service.ChannelCapacity}";
        if (partyButton) partyButton.GetComponentInChildren<TMP_Text>().text = $"{service.ChannelLabel} · {members.Length}/{service.ChannelCapacity}";
        var speaking = new List<string>();
        for (int i = 0; i < accounts.Length; ++i)
        {
            participantTexts[i].transform.parent.gameObject.SetActive(i < service.ChannelCapacity);
            bool exists = i < members.Length;
            accounts[i] = exists ? members[i].PlayerId : "";
            bool self = exists && members[i].IsSelf;
            bool muted = exists && (self ? service.MicrophoneMuted : service.ParticipantMuted(accounts[i]) || service.ParticipantVolume(accounts[i]) == 0);
            bool speakingNow = exists && !muted && (self ? service.Transmitting && members[i].SpeechDetected : members[i].SpeechDetected);
            participantTexts[i].text = exists ? members[i].DisplayName + (self ? "  (나)" : "") : "참가자 대기";
            participantStates[i].text = !exists ? "같은 파티·팀에 연결되면 표시됩니다" : muted ? "음소거됨" : speakingNow ? "말하는 중" : self ? "내 마이크 · 왼쪽에서 설정" : "연결됨";
            participantStates[i].color = speakingNow ? new Color(.33f,.85f,.72f) : new Color(.65f,.70f,.77f);
            participantActivity[i].color = muted ? new Color(.88f,.32f,.40f) : speakingNow ? new Color(.33f,.85f,.72f) : new Color(.27f,.33f,.40f);
            participantIcons[i].sprite = exists ? service.ParticipantIcon(members[i].DisplayName) : null;
            participantIcons[i].enabled = participantIcons[i].sprite != null;
            participantControls[i].SetActive(exists && !self);
            participantMuteButtons[i].interactable = participantVolumes[i].interactable = participantResetButtons[i].interactable = exists && !self;
            participantVolumes[i].SetValueWithoutNotify(exists && !self ? service.ParticipantVolume(accounts[i]) : 100);
            participantVolumeLabels[i].text = $"{participantVolumes[i].value:0}%";
            participantMuteButtons[i].GetComponentInChildren<TMP_Text>().text = exists && service.ParticipantMuted(accounts[i]) ? "음소거 해제" : "음소거";
            if (i < hudRows.Length)
            {
                hudRows[i].SetActive(exists);
                hudNames[i].text = exists ? members[i].DisplayName + (muted ? "  · 음소거" : "") : "";
                hudNames[i].color = speakingNow ? new Color(.33f,.85f,.72f) : new Color(.8f,.84f,.9f);
                hudIcons[i].sprite = participantIcons[i].sprite; hudIcons[i].enabled = hudIcons[i].sprite != null;
                hudActivity[i].color = participantActivity[i].color;
            }
            if (exists && members[i].SpeechDetected && !members[i].IsMuted) speaking.Add(members[i].DisplayName);
        }
        speakingText.text = hudRows.Length == 0 && speaking.Count > 0 ? "말하는 중  ·  " + string.Join(" / ", speaking) : "";
    }
    public void Toggle()
    {
        if (panel.activeSelf) { Close(); return; }
        if (!RefreshVisibility()) return;
        if (HasBlockingPopup || InGameChatUI.IsChatOpen) return;
        previousLock = Cursor.lockState; previousCursor = Cursor.visible;
        panel.SetActive(true); Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
    public static bool ShouldShow(string scene, bool authenticated, bool temporaryRoomVisible, bool privateRoomVisible) =>
        ProjectVector.Core.SceneCatalog.CanShowVoice(scene, authenticated, true, temporaryRoomVisible, privateRoomVisible);

    public static bool ShouldShowContext(string scene, bool authenticated, bool gameplayVisible, bool readyRoom, bool privateRoom) =>
        ProjectVector.Core.SceneCatalog.CanShowVoice(scene, authenticated, gameplayVisible, readyRoom, privateRoom);

    private bool RefreshVisibility()
    {
        var network = NetworkManager.Instance;
        bool show = ShouldShowContext(SceneManager.GetActiveScene().name,
            network != null && network.IsConnected && network.IsAuthenticated,
            MatchManager.Instance && MatchManager.Instance.IsGameplayVisible,
            network && network.CurrentLobbyRoomChatKind == LobbyRoomChatKind.ReadyRoom && partyView && partyView.activeInHierarchy,
            network && network.CurrentLobbyRoomChatKind == LobbyRoomChatKind.PrivateRoom && privateRoomView && privateRoomView.activeInHierarchy);
        if (openButton) openButton.gameObject.SetActive(show);
        if (partyButton) partyButton.gameObject.SetActive(show && SceneNavigation.Current == ProjectVector.Core.ProjectScene.Lobby);
        if (!show)
        {
            Close();
            if (speakingText) speakingText.text = "";
            foreach (var row in hudRows) if (row) row.SetActive(false);
        }
        return show;
    }
    public void Close()
    {
        if (panel == null || !panel.activeSelf) return;
        panel.SetActive(false); closedFrame = Time.frameCount; PlayerPrefs.Save();
        if (!HasBlockingPopup && !(SettingsMenu.Instance != null && SettingsMenu.Instance.IsOpen))
        { Cursor.lockState = previousLock; Cursor.visible = previousCursor; }
    }
}
