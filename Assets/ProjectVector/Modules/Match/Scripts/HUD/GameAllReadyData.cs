using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

public class GameReadyMemberData
{
    public uint playerId;
    public string userId;
    public string nickname;

    public byte team;
    public byte slotIndex;

    public int characterId;
    public int rankImageId;
    public Dictionary<int, int> weaponSkinLoadout = new Dictionary<int, int>();
}

public class GameAllReadyData
{
    public uint roomId;
    public string mapName;
    public ushort prepareSeconds;

    public List<GameReadyMemberData> members = new List<GameReadyMemberData>();
}
