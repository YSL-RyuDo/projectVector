using System.Collections.Generic;

[System.Serializable]
public class MatchLoadingMemberData
{
    public uint playerId;
    public string nickname;
    public byte slotIndex;
    public int characterId;
    public int rankImageId;
}

[System.Serializable]
public class MatchLoadingInfoData
{
    public uint roomId;

    public byte attackTeam;
    public byte defenseTeam;

    public List<MatchLoadingMemberData> attackMembers = new List<MatchLoadingMemberData>();
    public List<MatchLoadingMemberData> defenseMembers = new List<MatchLoadingMemberData>();
}