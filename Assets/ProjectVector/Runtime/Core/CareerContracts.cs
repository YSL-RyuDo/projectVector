using System.Collections.Generic;

public class CareerOverviewData
{
    public int seasonId;
    public string seasonName;
    public long seasonEndUnix;
    public int remainingSeasonDays;
    public int tierId;
    public string tierName;
    public int rankImageId;
    public int tierMinPoints;
    public int tierMaxPoints;
    public int rankPoints;
    public int rankedMatches;
    public int wins;
    public int roundsPlayed;
    public int kills;
    public int totalDamage;
    public int mvpCount;
}

public class CareerDetailSeasonData
{
    public int seasonId;
    public string seasonName;
    public bool isActive;
}

public class CareerDetailCharacterData
{
    public int characterId;
    public int performanceScore;
    public int matches;
    public int wins;
    public int kills;
    public int deaths;
    public int maxKills;
    public int maxDamage;
}

public class CareerDetailData
{
    public bool success;
    public bool hasData;
    public int seasonId;
    public int modeType;
    public int selectedCharacterId;
    public List<CareerDetailSeasonData> seasons = new();
    public List<CareerDetailCharacterData> characters = new();
    public int playCount;
    public int wins;
    public int roundsPlayed;
    public int totalDamage;
    public int kills;
    public int deaths;
    public int assists;
    public int headshotKills;
    public int meleeKills;
    public int spikePlants;
    public int spikeDefuses;
    public int maxAssists;
    public int maxRoundDamageHundredths;
    public int maxRoundKillsHundredths;
}
public class CareerRankingEntryData
{
    public int position;
    public string userId;
    public string nickname;
    public int playerIconId;
    public int tierId;
    public string tierName;
    public int rankImageId;
    public int rankPoints;
    public int rankedMatches;
    public int wins;
}

public class CareerRankingPageData
{
    public int rankingType;
    public int characterId;
    public int page;
    public int totalPages;
    public int totalEntries;
    public List<CareerRankingEntryData> entries = new();
}

public class CareerMatchPlayerData
{
    public string userId;
    public string nickname;
    public byte team;
    public int characterId;
    public int combatScore;
    public int attackScore;
    public int skillScore;
    public int teamworkScore;
    public int kills;
    public int deaths;
    public int assists;
    public int damage;
    public bool isMvp;
}

public class CareerMatchData
{
    public ulong matchId;
    public string mapName;
    public long startedAtUnix;
    public int durationSeconds;
    public byte scoreA;
    public byte scoreB;
    public byte winnerTeam;
    public byte myTeam;
    public List<CareerMatchPlayerData> players = new();
}

public class CareerInventoryEntryData
{
    public int itemId;
    public string itemName;
    public string description;
    public int iconId;
    public int quantity;
    public long acquiredAtUnix;
}

