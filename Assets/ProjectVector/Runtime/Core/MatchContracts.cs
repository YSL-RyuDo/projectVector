using System.Collections.Generic;

public enum PickPhase : byte
{
    PICKING = 0,
    PREPARE = 1,
    COMPLETE = 2
}

public enum RoundWinReason : byte
{
    NONE = 0,

    ATTACK_ELIMINATION = 1,
    ATTACK_SPIKE_EXPLODE = 2,

    DEFENSE_ELIMINATION = 3,
    DEFENSE_TIME_OUT = 4,
    DEFENSE_SPIKE_DEFUSE = 5
}

public class ScoreboardMemberData
{
    public uint playerId;
    public byte team;
    public byte slotIndex;
    public int characterId;
    public string nickname;

    public int kills;
    public int deaths;
    public int assists;

    public int credits;
    public bool hasLifeState;
    public bool hasLoadout;
    public int primaryWeaponId = -1, pistolWeaponId = -1, knifeWeaponId = -1;
    public int armor, maxArmor, ultimatePoints, ultimateRequired, ultimatePointPrice;
    public bool ultimateUsed;
    public bool hasEquipment;
    public int ownedWeaponId = -1;
    public bool equipmentAlive;
}

public class ScoreboardData
{
    public int firstHalfRounds, secondHalfRounds;
    public uint roomId;
    public byte myTeam;
    public List<ScoreboardMemberData> members = new();
}

public class PlayerCombatSnapshot
{
    public int hp;
    public int barrier;
    public int maxHp;
    public int maxBarrier;

    public bool hasHpState;

    public int weaponId = -1;
    public int magAmmo;
    public int reserveAmmo;

    public bool isADS;
}

