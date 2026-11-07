using System.Collections.Generic;

public class MyPageCharacterData
{
    public int characterId;
    public int matches;
    public int wins;
    public int maxKills;
    public bool owned;
}

public class MyPageVisitorData
{
    public string userId;
    public string nickname;
    public int playerIconId;
    public long visitedAtUnix;
}

public class MyPageProfileData
{
    public bool isSelf;
    public bool privacyEnabled;
    public string userId;
    public string nickname;
    public int level;
    public int playerIconId;
    public int logoBorderId;
    public string joinDate;
    public string region;
    public string birthday;
    public int likeCount;
    public int seasonId;
    public int currentRankImageId;
    public string currentTierName;
    public int seasonHighestRankImageId;
    public int allTimeHighestRankImageId;
    public int allTimeHighestSeasonId;
    public int playCount;
    public int wins;
    public int roundsPlayed;
    public int totalKills;
    public int totalAssists;
    public int totalDamage;
    public int mvpCount;
    public int headshotKills;
    public int spikePlants;
    public int spikeDefuses;
    public int weaponSkinOwnedCount;
    public int weaponSkinTotalCount;
    public int characterSkinOwnedCount;
    public int characterSkinTotalCount;
    public int[] battleStatIds = { 0, 1, 2, 3, 4, 5, 6 };
    public List<MyPageCharacterData> characters = new();
    public List<MyPageVisitorData> visitors = new();
}
public class ChipCraftMedalData
{
    public int medalId;
    public float x;
    public float y;
    public float rotation;
    public float scale = 1f;
    public bool flipped;
    public ChipCraftMedalData Clone() => (ChipCraftMedalData)MemberwiseClone();
}
public class ChipCraftPresetData
{
    public int cardId;
    public int caseId;
    public List<ChipCraftMedalData> medals = new();
    public ChipCraftPresetData Clone() => new()
    {
        cardId = cardId,
        caseId = caseId,
        medals = medals.ConvertAll(value => value.Clone())
    };
}
public class ChipCraftData
{
    public bool isSelf;
    public string userId;
    public int ownedCardMask;
    public int ownedCaseMask;
    public int ownedMedalMask;
    public ChipCraftPresetData preset = new();
}
