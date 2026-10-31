using System.Collections.Generic;

public enum PickRole : byte
{
    ATTACK = 0,
    DEFENSE = 1
}

public enum PickLockResult : byte
{
    SUCCESS = 0,
    NOT_IN_GAME = 1,
    ROOM_NOT_FOUND = 2,
    INVALID_CHARACTER = 3,
    ALREADY_LOCKED = 4,
    CHARACTER_ALREADY_TAKEN = 5,
    FAILED = 6
}

public class PickTeamMemberData
{
    public uint playerId;
    public string nickname;

    public byte slotIndex;

    public int hoverCharacterId;
    public int lockedCharacterId;

    public bool isLocked;
    public bool isMe;
}

public class PickStateData
{
    public uint roomId;
    public string mapName;

    public RoomSlotTeam myTeam;
    public PickRole firstRole;
    public PickRole secondRole;

    public ushort remainingSeconds;

    public byte blueScore;
    public byte redScore;

    public List<PickTeamMemberData> members = new List<PickTeamMemberData>();
}