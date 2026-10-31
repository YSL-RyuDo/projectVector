using System.Collections.Generic;

public class LobbyCharacterData
{
    public int characterId;
    public bool owned;
}

public class TempGamePlayerData
{
    public int slotIndex;
    public string nickname;
    public int playerIconId;
    public int logoBorderId;
    public int rankImageId;
    public ChipCraftPresetData preset = new();
}

public class RoomListData
{
    public uint roomId;
    public string roomName;
    public string mapName;
    public string hostName;
    public ushort currentPlayerCount;
    public ushort maxPlayerCount;
    public byte state;
}

public enum RoomSlotTeam : byte
{
    A = 0,
    B = 1,
    Observer = 2
}

public class RoomMemberData
{
    public uint playerId;
    public string nickname;
    public int level;
    public int playerIconId;

    public RoomSlotTeam team;
    public byte slotIndex;
    public bool isLeader;
}

public class RoomStateData
{
    public uint roomId;
    public string roomName;
    public string mapName;

    public bool isLeader;
    public bool canStart;

    public List<RoomMemberData> members = new List<RoomMemberData>();
}

public enum RoomEnterResult : byte
{
    SUCCESS = 0,
    ROOM_NOT_FOUND = 1,
    ROOM_FULL = 2,
    ALREADY_IN_ROOM = 3,
    PLAYER_DATA_NOT_FOUND = 4,
    NOT_IN_LOBBY = 5
}

public enum RoomSlotChangeResult : byte
{
    SUCCESS = 0,
    REQUEST_SENT = 1,
    NOT_IN_ROOM = 2,
    ROOM_NOT_FOUND = 3,
    INVALID_SLOT = 4,
    SAME_SLOT = 5,
    TARGET_NOT_FOUND = 6,
    REQUEST_ALREADY_EXISTS = 7,
    REJECTED = 8,
    FAILED = 9
}

public class RoomSlotSwapRequestData
{
    public uint requesterPlayerId;
    public string requesterNickname;

    public RoomSlotTeam requesterTeam;
    public byte requesterSlotIndex;

    public RoomSlotTeam targetTeam;
    public byte targetSlotIndex;
}

public enum RoomAdminResult : byte
{
    SUCCESS = 0,
    NOT_IN_ROOM = 1,
    ROOM_NOT_FOUND = 2,
    NOT_LEADER = 3,
    TARGET_NOT_FOUND = 4,
    CANNOT_TARGET_SELF = 5,
    FAILED = 6
}

public enum RoomGameStartResult : byte
{
    SUCCESS = 0,
    NOT_IN_ROOM = 1,
    ROOM_NOT_FOUND = 2,
    NOT_LEADER = 3,
    NOT_ENOUGH_PLAYERS = 4,
    ALREADY_PLAYING = 5,
    FAILED = 6,
    NOT_ALL_RETURNED_FROM_RESULT = 7
}

