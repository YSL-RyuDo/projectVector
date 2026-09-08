using System;

public enum FriendPresenceStatus : byte
{
    Offline = 0,
    Online = 1,
    DoNotDisturb = 2
}

public enum FriendPresenceMode : byte
{
    Online = 0,
    DoNotDisturb = 1
}

public enum FriendOperation : byte
{
    SendRequest = 0,
    CancelRequest = 1,
    AcceptRequest = 2,
    RejectRequest = 3,
    Block = 4,
    Unblock = 5,
    DeleteFriend = 6
}

public enum FriendOperationResult : byte
{
    Success = 0,
    Invalid = 1,
    Self = 2,
    AlreadyFriend = 3,
    AlreadyPending = 4,
    Blocked = 5,
    NotPending = 6,
    NotFriend = 7
}

[Serializable]
public sealed class FriendUserData
{
    public uint playerId;
    public string userId;
    public string nickname;
    public int level;
    public int playerIconId;
    public int rankImageId;
    public FriendPresenceStatus presenceStatus;
    public string memo;
}

[Serializable]
public sealed class FriendRequestData
{
    public bool incoming;
    public FriendUserData user;
}
