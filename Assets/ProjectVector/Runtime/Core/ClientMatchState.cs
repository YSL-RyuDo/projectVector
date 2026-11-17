using System;

// 클라이언트 규칙 구분용 값이며 서버 패킷의 모드 번호와는 별개다.
public enum ClientMatchMode { Standard, Ranked, Deathmatch }
public enum ClientMatchPhase { Inactive, Preparing, Playing, Ended }
public enum ClientLifeState { Alive, WaitingForRespawn }

// 클라이언트 상태만 관리하며 지갑, 생성된 무기, 서버 인벤토리는 변경하지 않는다.
public sealed class ClientMatchState
{
    public ClientMatchMode Mode { get; private set; }
    public ClientMatchPhase Phase { get; private set; }
    public ClientLifeState Life { get; private set; }
    public ulong MatchId { get; private set; }
    public uint Revision { get; private set; }
    public bool CharacterAssigned { get; private set; }
    public bool IsDeathmatch => Mode == ClientMatchMode.Deathmatch;
    public bool AllowsSkills => !IsDeathmatch;
    public bool AllowsArmor => !IsDeathmatch;
    public bool AllowsTeamPurchases => !IsDeathmatch;

    // 개인의 사망으로 경기 전체가 구매 단계로 바뀌지 않도록 개인 생명 상태를 따로 확인한다.
    public bool CanSelectDeathmatchWeapon => IsDeathmatch && CharacterAssigned &&
        (Phase == ClientMatchPhase.Preparing ||
         (Phase == ClientMatchPhase.Playing && Life == ClientLifeState.WaitingForRespawn));

    // 부활용 무기 선택 초안이며, 서버 연동 시 검증과 승인 처리를 추가해야 한다.
    // 바닥 무기 획득, G키 드롭, 현재 생명의 인벤토리 변경은 이 선택에 반영하지 않는다.
    public int SelectedPrimary { get; private set; } = -1;
    public int SelectedPistol { get; private set; } = 0;
    public int SelectedMelee => 13;
    public event Action Changed;

    public void BeginMatch(ClientMatchMode mode, ulong matchId)
    {
        if (!Enum.IsDefined(typeof(ClientMatchMode), mode) || matchId == 0)
            throw new ArgumentOutOfRangeException();
        Mode = mode;
        MatchId = matchId;
        Phase = ClientMatchPhase.Inactive;
        Life = ClientLifeState.Alive;
        Revision = 0;
        CharacterAssigned = false;
        SelectedPrimary = -1;
        SelectedPistol = 0;
        Changed?.Invoke();
    }

    // 서버 수신 연결부 또는 에디터 미리보기에서 메인 스레드로 호출한다.
    // 이전 경기나 오래된 상태가 부활 또는 결과 표시 후 상점을 다시 열지 못하게 한다.
    public bool ApplySnapshot(ulong matchId, uint revision, ClientMatchPhase phase,
        ClientLifeState life, bool characterAssigned)
    {
        if (MatchId == 0 || matchId != MatchId || revision <= Revision ||
            !Enum.IsDefined(typeof(ClientMatchPhase), phase) ||
            !Enum.IsDefined(typeof(ClientLifeState), life) || phase < Phase ||
            (CharacterAssigned && !characterAssigned)) return false;
        Revision = revision;
        Phase = phase;
        Life = life;
        CharacterAssigned = characterAssigned;
        Changed?.Invoke();
        return true;
    }

    public bool IsSelected(int weaponId) => weaponId >= 0 &&
        (weaponId == SelectedPrimary || weaponId == SelectedPistol || weaponId == SelectedMelee);

    public bool TrySelectWeapon(int weaponId)
    {
        if (!CanSelectDeathmatchWeapon || weaponId < 0 || weaponId > 12) return false;
        if (weaponId <= 2) SelectedPistol = weaponId;
        else SelectedPrimary = weaponId;
        Changed?.Invoke();
        return true;
    }

    public void Reset()
    {
        Mode = ClientMatchMode.Standard;
        Phase = ClientMatchPhase.Inactive;
        Life = ClientLifeState.Alive;
        MatchId = 0;
        Revision = 0;
        CharacterAssigned = false;
        SelectedPrimary = -1;
        SelectedPistol = 0;
        Changed?.Invoke();
    }
}
