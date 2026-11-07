using UnityEngine;

public partial class PickManager
{
    [SerializeField] PickInteractionView interactionView;
    PickActionsState actionState;
    bool interactionFinished;
    float nextActionClick;
    void InitializeInteractions()
    {
        actionState = null; interactionFinished = false;
        foreach (var slot in teamPickSlots) if (slot != null) slot.BindActions(RequestExchange, ShareMyRecord);
        if (interactionView == null) return;
        interactionView.Hide();
        interactionView.acceptButton.onClick.AddListener(() => ReplyExchange(1));
        interactionView.declineButton.onClick.AddListener(() => ReplyExchange(2));
        interactionView.cancelButton.onClick.AddListener(() => ReplyExchange(3));
    }
    void ReconcilePickActions()
    {
        if (actionState != null && currentState != null && actionState.roomId != currentState.roomId)
        {
            actionState = null; interactionFinished = false; nextActionClick = 0;
            interactionView?.Hide();
        }
        if (actionState == null || currentState == null) return;
        foreach (var m in currentState.members)
        {
            var a = actionState.members.Find(x => x.playerId == m.playerId);
            if (a == null || !a.locked) continue;
            m.isLocked = true; m.lockedCharacterId = m.hoverCharacterId = a.characterId;
        }
    }
    void RefreshPickActions()
    {
        if (currentState == null) return;
        var me = GetMe();
        var mine = actionState?.members.Find(x => x.playerId == me?.playerId);
        bool open = !interactionFinished && actionState != null && actionState.roomId == currentState.roomId && actionState.open;
        foreach (var member in currentState.members)
        {
            if (member.slotIndex >= teamPickSlots.Length) continue;
            var a = actionState?.members.Find(x => x.playerId == member.playerId);
            bool busy = a != null && a.exchangeId != 0;
            teamPickSlots[member.slotIndex]?.SetActions(member.playerId, member.isMe, member.isLocked,
                open && mine != null && mine.locked && mine.exchangeId == 0 && !busy, busy);
            if (busy && presentation != null && member.slotIndex < presentation.teammateStates.Length)
                presentation.teammateStates[member.slotIndex].text = "교환 진행 중";
        }
        if (!open || mine == null || mine.exchangeId == 0)
        { interactionView?.SetExchange(false, false, null, null, "", 0); return; }
        var peer = currentState.members.Find(x => x.playerId == mine.peerId);
        if (peer == null) return;
        interactionView?.SetExchange(true, mine.requester, GetCharacterData(mine.characterId)?.characterImage,
            GetCharacterData(peer.lockedCharacterId)?.characterImage,
            $"{me.nickname}  ↔  {peer.nickname}", mine.remainingMs / 1000f);
    }
    public void OnRecvPickActions(PickActionsState state)
    {
        if (interactionFinished || currentState == null || currentState.roomId != state.roomId || (actionState != null && state.revision < actionState.revision)) return;
        // Reject identities not in our server-supplied allied selection roster.
        if (state.members.Exists(x => !currentState.members.Exists(m => m.playerId == x.playerId))) return;
        int oldCharacter = GetMe()?.lockedCharacterId ?? -1;
        ulong oldExchange = actionState?.members.Find(x => x.playerId == GetMe()?.playerId)?.exchangeId ?? 0;
        actionState = state; ReconcilePickActions();
        if (presentation != null) presentation.SetState(currentState);
        ApplyTeamPickSlots(); ApplyCharacterButtons(); ApplySelectButton(); ApplyCurrentCharacterInfoSafely(); RefreshPickActions();
        if (oldExchange != 0 && (actionState.members.Find(x => x.playerId == GetMe()?.playerId)?.exchangeId ?? 0) == 0)
            interactionView?.Notice(oldCharacter != GetMe()?.lockedCharacterId ? "교환 완료" : "교환 종료",
                oldCharacter != GetMe()?.lockedCharacterId ? "교환한 캐릭터로 출전합니다" : state.open ? "요청이 거절, 취소되었거나 시간이 만료되었습니다" : "전투 준비가 완료되었습니다");
    }
    void RequestExchange(uint target)
    {
        if (currentState == null || Time.unscaledTime < nextActionClick) return;
        nextActionClick = Time.unscaledTime + .5f;
        NetworkManager.Instance?.SendPickAction(currentState.roomId, 0, target);
    }
    void ReplyExchange(byte action)
    {
        var mine = actionState?.members.Find(x => x.playerId == GetMe()?.playerId);
        if (mine == null || mine.exchangeId == 0) return;
        NetworkManager.Instance?.SendPickAction(currentState.roomId, action, 0, mine.exchangeId);
    }
    void ShareMyRecord()
    {
        if (currentState == null || Time.unscaledTime < nextActionClick) return;
        nextActionClick = Time.unscaledTime + .5f;
        NetworkManager.Instance?.SendPickAction(currentState.roomId, 4);
    }
    public void OnPickActionResult(uint room, byte code)
    {
        if (interactionFinished || currentState?.roomId != room || code == 0) return;
        string text = code switch { 1 => "캐릭터 교환 시간이 끝났습니다", 2 => "선택을 확정한 아군과 교환할 수 있습니다", 3 => "이미 교환을 진행 중인 플레이어입니다",
            4 => "이미 종료된 교환 요청입니다", 5 => "승률 또는 플레이 수가 부족합니다", _ => "잠시 후 다시 시도해 주세요" };
        interactionView?.Notice(code == 5 ? "전적 자랑" : "알림", text);
    }
    public void OnPickShowcase(uint room, uint player, int character, uint matches, uint wins, byte kind, uint kills)
    {
        if (interactionFinished || currentState?.roomId != room) return;
        var member = currentState.members.Find(x => x.playerId == player);
        if (member == null) return;
        var data = GetCharacterData(character);
        string record = $"{matches}전 {wins}승, 승률 {(100UL * wins / matches)}%";
        string recent = kind == 1 ? "최근 경기 MVP, " : kind == 2 ? $"최근 경기 {kills}킬, " : "";
        interactionView?.Notice($"{member.nickname}  /  {data?.characterName}", recent + record, data?.characterImage);
    }
    void FinishPickInteractions() { interactionFinished = true; interactionView?.Hide(); RefreshPickActions(); }
}
