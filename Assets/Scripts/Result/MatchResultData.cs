using System.Collections.Generic;

public class MatchResultMemberData
{
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
}

public static class MatchResultCache
{
    public static byte winnerTeam;
    public static byte scoreA;
    public static byte scoreB;

    public static readonly List<MatchResultMemberData> members = new();

    public static void Clear()
    {
        winnerTeam = 0;
        scoreA = 0;
        scoreB = 0;
        members.Clear();
    }
}