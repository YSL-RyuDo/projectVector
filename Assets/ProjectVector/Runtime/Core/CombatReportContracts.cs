using System.Collections.Generic;

public class CombatReportRawEntry
{
    public uint opponentId;
    public int characterId;
    public string nickname;
    public byte sourceKind; // 0: 무기, 1: 스킬
    public byte direction; // 0: 내가 준 피해, 1: 내가 받은 피해
    public int sourceId; // 무기 ID 또는 스킬 ID
    public uint hpDamage;
    public uint barrierDamage;
    public uint bodyHits;
    public uint headHits;
    public uint legHits;
    public uint unlocalizedHits; // 스킬 등 부위 없는 피해 횟수
}

public class OpponentCombatSummary
{
    public uint opponentId;
    public int characterId;
    public string nickname;
    public bool isKiller;

    // 내가 준 총 피해 & 부위별 타격수
    public ulong damageDealt;
    public ulong hitsDealtHead;
    public ulong hitsDealtBody;
    public ulong hitsDealtLeg;

    // 내가 받은 총 피해 & 부위별 피격수
    public ulong damageTaken;
    public ulong hitsTakenHead;
    public ulong hitsTakenBody;
    public ulong hitsTakenLeg;

    // 무기/스킬별 상세 내역
    public List<CombatReportRawEntry> details = new();
}

