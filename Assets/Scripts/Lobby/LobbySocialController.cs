using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All views and buttons are authored in Lobby.unity; no runtime UI construction.
public sealed class LobbySocialController : MonoBehaviour
{
    public static LobbySocialController Instance { get; private set; }
    public GameObject invitePanel, readyPanel, confirmationPanel, toastPanel;
    public TMP_Text inviteText, inviteTimer, readyTimer, readyProgress, readyButtonText, confirmationText, toastText, partyText;
    public Button acceptInvite, declineInvite, acceptReady, declineReady, confirmButton, cancelButton, leaveParty;
    public Button[] inviteSlots;
    public Image[] readyCards;
    public TMP_Text[] readyCardLabels;
    public FriendPanelController friends;
    public GamePanelController game;
    private uint inviteToken, readyToken;
    private float inviteDeadline, readyDeadline, toastDeadline;
    private bool readyAnswered, connected, awaitingJoin;
    private Action confirmed;
    private int dismissedFrame = -1;
    public bool HasModal => (invitePanel != null && invitePanel.activeSelf) || (readyPanel != null && readyPanel.activeSelf) || (confirmationPanel != null && confirmationPanel.activeSelf);
    public bool BlocksProfileBack => HasModal || dismissedFrame == Time.frameCount;

    private void Awake()
    {
        Instance = this;
        acceptInvite.onClick.AddListener(() => ReplyInvite(true));
        declineInvite.onClick.AddListener(() => ReplyInvite(false));
        acceptReady.onClick.AddListener(() => ReplyReady(true));
        declineReady.onClick.AddListener(() => ReplyReady(false));
        confirmButton.onClick.AddListener(() => { var action = confirmed; CloseConfirmation(); action?.Invoke(); });
        cancelButton.onClick.AddListener(CloseConfirmation);
        leaveParty.onClick.AddListener(() => Confirm("파티에서 나가시겠습니까?", () => NetworkManager.Instance?.SendSocial(PacketType.PKT_CS_PARTY_LEAVE)));
        foreach (var button in inviteSlots) button.onClick.AddListener(() => friends.OpenPanel());
        invitePanel.SetActive(false); readyPanel.SetActive(false); confirmationPanel.SetActive(false); toastPanel.SetActive(false);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void Start() { NetworkManager.Instance?.SendSocial(PacketType.PKT_CS_PARTY_STATE); RefreshParty(); }

    private void Update()
    {
        var network = NetworkManager.Instance;
        bool online = network != null && network.IsConnected && network.IsAuthenticated;
        if (!online && connected)
        {
            invitePanel.SetActive(false); readyPanel.SetActive(false); CloseConfirmation();
            inviteToken = readyToken = 0;
            Toast("서버 연결이 끊어졌습니다. 다시 로그인해 주세요.");
        }
        if (online && !connected) network.SendSocial(PacketType.PKT_CS_PARTY_STATE);
        connected = online;
        if (toastPanel.activeSelf && Time.unscaledTime >= toastDeadline) toastPanel.SetActive(false);
        if (invitePanel.activeSelf)
        {
            int left = Mathf.Max(0, Mathf.CeilToInt(inviteDeadline - Time.unscaledTime));
            inviteTimer.text = $"{left}초 후 초대 만료";
            if (left == 0) { invitePanel.SetActive(false); inviteToken = 0; Toast("초대 시간이 만료되었습니다."); }
        }
        if (readyPanel.activeSelf)
        {
            int left = Mathf.Max(0, Mathf.CeilToInt(readyDeadline - Time.unscaledTime));
            readyTimer.text = left > 0 ? $"<color=#ED536A>{left:00}</color>초 내에 확인해 주세요" : "서버 응답을 기다리는 중입니다";
            acceptReady.interactable = !readyAnswered && left > 0 && online;
            if (Input.GetKeyDown(KeyCode.Space) && acceptReady.interactable) ReplyReady(true);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (confirmationPanel.activeSelf) CloseConfirmation();
            else if (invitePanel.activeSelf) ReplyInvite(false);
        }
        RefreshParty();
    }

    public void RefreshParty()
    {
        var net = NetworkManager.Instance;
        int count = net == null ? 1 : Mathf.Max(1, net.PartyPlayers.Count);
        bool leader = net == null || net.IsPartyLeader;
        partyText.text = count > 1 ? $"파티 {count} / 3  ·  {(leader ? "팀 리더" : "리더의 시작을 기다리는 중")}" : "";
        if (count > 1 && net != null && net.PartyAvailabilityKnown && !net.PartyReadyToQueue)
            partyText.text = $"파티 {count} / 3  ·  로비 복귀 {net.PartyLobbyCount} / {count}";
        leaveParty.gameObject.SetActive(count > 1);
        leaveParty.interactable = !GamePanelController.IsMatchmakingActive && !readyPanel.activeSelf;
        for (int i = 0; i < inviteSlots.Length; ++i)
        {
            inviteSlots[i].gameObject.SetActive(i + 1 >= count);
            inviteSlots[i].interactable = leader && !GamePanelController.IsMatchmakingActive;
        }
    }

    public void Invite(FriendUserData user)
    {
        if (user == null || user.playerId == 0 || user.presenceStatus != FriendPresenceStatus.Online) { Toast("지금은 초대할 수 없는 친구입니다."); return; }
        if (GamePanelController.IsMatchmakingActive) { Toast("매칭을 취소한 뒤 초대해 주세요."); return; }
        NetworkManager.Instance?.SendSocial(PacketType.PKT_CS_PARTY_INVITE, user.playerId);
    }

    public void ReceiveInvite(uint token, string nickname, int seconds)
    {
        if (readyPanel.activeSelf) { NetworkManager.Instance?.SendSocial(PacketType.PKT_CS_PARTY_INVITE_REPLY, token, false); return; }
        inviteToken = token; inviteDeadline = Time.unscaledTime + seconds;
        inviteText.richText = false;
        inviteText.text = $"{nickname} 님이 파티에 초대했습니다.\n함께 일반전 또는 랭크전에 참여하세요.";
        invitePanel.SetActive(true); invitePanel.transform.SetAsLastSibling();
    }

    private void ReplyInvite(bool accept)
    {
        if (inviteToken == 0) return;
        NetworkManager.Instance?.SendSocial(PacketType.PKT_CS_PARTY_INVITE_REPLY, inviteToken, accept);
        awaitingJoin = accept;
        inviteToken = 0; invitePanel.SetActive(false);
        dismissedFrame = Time.frameCount;
    }

    public void ReceiveReady(uint token, byte phase, int seconds, int total, int accepted, bool selfAccepted)
    {
        if (phase > 2 || total != 6 || accepted > total) return;
        if (phase != 0)
        {
            if (readyToken != 0 && readyToken != token) return;
            readyPanel.SetActive(false); readyToken = 0;
            if (phase == 2) Toast("준비 확인이 완료되지 않아 매칭이 취소되었습니다.");
            return;
        }
        if (readyToken != token) { readyAnswered = false; declineReady.interactable = true; }
        readyToken = token; readyDeadline = Time.unscaledTime + seconds;
        readyAnswered |= selfAccepted;
        readyButtonText.text = readyAnswered ? "확인 완료 · 팀원 대기" : "Space   준비 확인";
        readyProgress.text = $"{accepted} / {total} 준비 완료";
        for (int i = 0; i < readyCards.Length; ++i)
        {
            readyCards[i].color = i < accepted ? new Color32(245, 241, 228, 255) : new Color32(130, 133, 139, 255);
            readyCardLabels[i].text = i < accepted ? "READY" : "대기 중";
        }
        invitePanel.SetActive(false); CloseConfirmation();
        if (!readyPanel.activeSelf) { readyPanel.SetActive(true); readyPanel.transform.SetAsLastSibling(); }
    }

    private void ReplyReady(bool accept)
    {
        if (readyToken == 0 || (accept && readyAnswered)) return;
        NetworkManager.Instance?.SendSocial(PacketType.PKT_CS_MATCH_READY, readyToken, accept);
        if (accept) { readyAnswered = true; acceptReady.interactable = false; readyButtonText.text = "확인 전송 중"; }
        else declineReady.interactable = false;
    }

    public void ReceiveResult(byte result)
    {
        if (awaitingJoin)
        {
            awaitingJoin = false;
            if (result == 1) { friends.ClosePanel(); MyPagePanelController.Instance?.Close(); game.OpenPartyLobby(); }
        }
        string[] messages = {"파티 초대를 보냈습니다.", "파티에 합류했습니다.", "친구가 초대를 거절했습니다.", "요청을 처리할 수 없습니다.", "매칭 중이거나 파티에 참여 중입니다. 매칭 취소 또는 파티 나가기 후 다시 시도해 주세요.", "친구 관계를 확인해 주세요.", "상대가 접속 중이 아니거나 초대를 받을 수 없습니다.", "초대 또는 준비 확인 시간이 만료되었습니다.", "파티가 가득 찼거나 더 이상 참여할 수 없습니다.", "파티 리더만 초대와 매칭을 시작할 수 있습니다."};
        Toast(result < messages.Length ? messages[result] : "요청을 처리할 수 없습니다.");
    }

    public void Confirm(string message, Action action)
    {
        if (readyPanel.activeSelf) return;
        confirmed = action; confirmationText.text = message; confirmationText.richText = false;
        confirmationPanel.SetActive(true); confirmationPanel.transform.SetAsLastSibling();
    }
    private void CloseConfirmation() { if (confirmationPanel.activeSelf) dismissedFrame = Time.frameCount; confirmed = null; confirmationPanel.SetActive(false); }
    public void Toast(string message) { toastText.text = message; toastText.richText = false; toastDeadline = Time.unscaledTime + 4f; toastPanel.SetActive(true); toastPanel.transform.SetAsLastSibling(); }
}
