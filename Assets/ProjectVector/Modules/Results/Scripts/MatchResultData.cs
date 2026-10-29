using System.Collections.Generic;

public class MatchResultMemberData
{
    public MatchAppearanceData appearance;
    public uint playerId;
    public byte team;
    public byte slotIndex;
    public int characterId;
    public string nickname;

    public int kills;
    public int deaths;
    public int assists;

    public int firstHalfKills;
    public int firstHalfDeaths;
    public int firstHalfAssists;

    public int secondHalfKills;
    public int secondHalfDeaths;
    public int secondHalfAssists;

    public int overtimeKills;
    public int overtimeDeaths;
    public int overtimeAssists;

    public bool isMvp;
    public int freeCurrencyReward;
    public int victoryCurrencyReward;
    public int rankPointDelta;
    public int rankPointsBefore;
    public int rankPointsAfter;
    public int rankTierIdBefore;
    public int rankTierIdAfter;
    public ushort rankExpectedWinRatePermille;
    public byte rankModifierFlags;
    public int totalFreeCurrency;
    public int totalVictoryCurrency;
}

public class MatchAppearanceData
{
    public int playerIconId; // One selected ID: PlayerIcon/N and IconCard/iconN_card.
    public int logoBorderId;
    public int rankImageId;
    public int level;
    public ChipCraftPresetData preset;
}

public static class MatchResultCache
{
    public static string mapName;
    public static byte winnerTeam;
    public static byte scoreA;
    public static byte scoreB;
    public static byte matchType;

    public static readonly List<MatchResultMemberData> members = new();
    public static int freeCurrencyReward;
    public static int victoryCurrencyReward;
    public static int rankPointDelta;
    public static int rankPointsBefore;
    public static int rankPointsAfter;
    public static int rankTierIdBefore;
    public static int rankTierIdAfter;
    public static ushort rankExpectedWinRatePermille;
    public static byte rankModifierFlags;
    public static int totalFreeCurrency;
    public static int totalVictoryCurrency;

    public static void Clear()
    {
        mapName = null;
        winnerTeam = 0;
        scoreA = 0;
        scoreB = 0;
        matchType = 0;
        freeCurrencyReward = 0;
        victoryCurrencyReward = 0;
        rankPointDelta = 0;
        rankPointsBefore = 0;
        rankPointsAfter = 0;
        rankTierIdBefore = 0;
        rankTierIdAfter = 0;
        rankExpectedWinRatePermille = 0;
        rankModifierFlags = 0;
        totalFreeCurrency = 0;
        totalVictoryCurrency = 0;
        members.Clear();
    }
}
