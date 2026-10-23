using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using System.Security.Principal;
using System.Text;

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
}

public class ScoreboardData
{
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

public partial class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    NetworkClient client = new NetworkClient();
    Queue<(ushort, byte[])> packetQueue = new();

    LoginUI loginUI;

    LobbyPanelController lobbyPanelController;
    GamePanelController gamePanelController;
    ProfilePanelController profilePanelController;
    ChatPanelController chatPanelController;
    FriendPanelController friendPanelController;
    LobbyManager lobbyManager;

    private LobbyRoomChatKind lobbyRoomChatKind = LobbyRoomChatKind.None;
    private uint lobbyRoomChatId;
    private string lobbyRoomChatName = "";

    public LobbyRoomChatKind CurrentLobbyRoomChatKind => lobbyRoomChatKind;
    public uint CurrentLobbyRoomChatId => lobbyRoomChatId;
    public string CurrentLobbyRoomChatName => lobbyRoomChatName;

    private PickManager pickManager;
    private MatchManager matchManager;
    private ScoreboardData latestScoreboardData;

    private uint myPlayerId;
    public uint MyPlayerId => myPlayerId;
    public PlayerIdentity MyPlayer => FindPlayerIdentityById(myPlayerId);

    private string myUserId = "";
    public string MyUserId => myUserId;
    public bool IsConnected => client != null && client.IsConnected;
    public bool IsAuthenticated { get; private set; }

    private uint pendingReturnRoomId = 0;

    public bool HasPendingReturnRoom => pendingReturnRoomId != 0;
    public uint PendingReturnRoomId => pendingReturnRoomId;

    private readonly Dictionary<uint, GameObject> droppedItemMap = new();
    private readonly Dictionary<uint, PlayerCombatSnapshot> playerCombatSnapshots = new();
    private const int WeaponSkinWeaponCount = 14;
    private const int WeaponSkinCharacterCount = 4;
    private readonly Dictionary<int, Dictionary<int, int>> equippedWeaponSkinsByCharacter = new();

    public event Action<int> WeaponSkinLoadoutChanged;

    public bool HasWeaponSkinLoadout(int characterId)
    {
        return equippedWeaponSkinsByCharacter.ContainsKey(characterId);
    }

    public int GetEquippedWeaponSkinId(int characterId, int weaponId)
    {
        return equippedWeaponSkinsByCharacter.TryGetValue(
                   characterId,
                   out Dictionary<int, int> loadout) &&
               loadout.TryGetValue(weaponId, out int skinId)
            ? skinId
            : 0;
    }

    public Dictionary<int, int> CopyEquippedWeaponSkins(int characterId)
    {
        return equippedWeaponSkinsByCharacter.TryGetValue(
            characterId,
            out Dictionary<int, int> loadout)
            ? new Dictionary<int, int>(loadout)
            : new Dictionary<int, int>();
    }
    public void ClearPendingReturnRoom()
    {
        pendingReturnRoomId = 0;
    }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        client.OnPacket += (type, body) =>
        {
            lock (packetQueue)
            {
                packetQueue.Enqueue((type, body));
            }
        };

        client.Connect("127.0.0.1", 9000);
        //client.Connect("211.247.47.240", 9000);
    }

    void Update()
    {
        UpdateReconnect();
        while (true)
        {
            (ushort, byte[]) packet;

            lock (packetQueue)
            {
                if (packetQueue.Count == 0)
                    break;

                packet = packetQueue.Dequeue();
            }

            HandlePacket(packet.Item1, packet.Item2);
        }
    }

    public void SetLoginUI(LoginUI ui)
    {
        loginUI = ui;
    }

    public void SetLobbyManager(LobbyManager manager)
    {
        lobbyManager = manager;
    }

    public void SetLobbyPanelController(LobbyPanelController controller)
    {
        lobbyPanelController = controller;
    }

    public void ClearLobbyPanelController(LobbyPanelController controller)
    {
        if (ReferenceEquals(lobbyPanelController, controller))
            lobbyPanelController = null;
    }

    public void SetGamePanelController(GamePanelController controller)
    {
        gamePanelController = controller;
    }

    public void SetProfilePanelController(ProfilePanelController controller)
    {
        profilePanelController = controller;
    }

    public void SetChatPanelController(ChatPanelController controller)
    {
        chatPanelController = controller;
        SyncLobbyRoomChatContext();
    }

    public void ClearChatPanelController(ChatPanelController controller)
    {
        if (ReferenceEquals(chatPanelController, controller))
            chatPanelController = null;
    }

    public void SetFriendPanelController(FriendPanelController controller)
    {
        friendPanelController = controller;
    }

    public void ClearFriendPanelController(FriendPanelController controller)
    {
        if (ReferenceEquals(friendPanelController, controller))
            friendPanelController = null;
    }

    public void SetReadyRoomChatContext()
    {
        lobbyRoomChatKind = LobbyRoomChatKind.ReadyRoom;
        lobbyRoomChatId = 0;
        lobbyRoomChatName = "준비방";
        SyncLobbyRoomChatContext();
    }

    public void SetPrivateRoomChatContext(uint roomId, string roomName)
    {
        if (roomId == 0)
            return;

        lobbyRoomChatKind = LobbyRoomChatKind.PrivateRoom;
        lobbyRoomChatId = roomId;
        lobbyRoomChatName = roomName ?? "";
        SyncLobbyRoomChatContext();
    }

    public void ClearTemporaryRoomChatContext()
    {
        if (lobbyRoomChatKind == LobbyRoomChatKind.ReadyRoom)
            ClearLobbyRoomChatContext();
    }

    public void ClearPrivateRoomChatContext(uint roomId = 0)
    {
        if (lobbyRoomChatKind != LobbyRoomChatKind.PrivateRoom)
            return;

        if (roomId != 0 && lobbyRoomChatId != roomId)
            return;

        ClearLobbyRoomChatContext();
    }

    public void ClearLobbyRoomChatContext()
    {
        lobbyRoomChatKind = LobbyRoomChatKind.None;
        lobbyRoomChatId = 0;
        lobbyRoomChatName = "";
        SyncLobbyRoomChatContext();
    }

    private void SyncLobbyRoomChatContext()
    {
        chatPanelController?.SetRoomChatContext(
            lobbyRoomChatKind,
            lobbyRoomChatId,
            lobbyRoomChatName
        );
    }

    public void SetMatchManager(MatchManager manager)
    {
        matchManager = manager;
    }

    public void SendRegister(string id, string nick, string pw, string confirm)
    {
        byte[] buffer = new byte[4 + 20 + 20 + 64 + 64];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_REGISTER).CopyTo(buffer, 2);

        Packet.WriteFixedString(buffer, 4, id, 20);
        Packet.WriteFixedString(buffer, 24, nick, 20);
        Packet.WriteFixedString(buffer, 44, pw, 64);
        Packet.WriteFixedString(buffer, 108, confirm, 64);

        client.Send(buffer);
    }

    public void SendLogin(string id, string pw)
    {
        reconnectAccount = id.Trim();
        reconnectPassword = pw; // Process memory only; never PlayerPrefs, files or logs.
        IsAuthenticated = false;
        ResetSocialState();
        myUserId = id.Trim();
        equippedWeaponSkinsByCharacter.Clear();
        ClearLobbyRoomChatContext();

        byte[] buffer = new byte[4 + 20 + 64];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOGIN).CopyTo(buffer, 2);

        Packet.WriteFixedString(buffer, 4, id, 20);
        Packet.WriteFixedString(buffer, 24, pw, 64);

        client.Send(buffer);
    }

    public void SendLobbyEnter()
    {
        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_ENTER).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    public void SendLobbyChat(string message)
    {
        byte[] buffer = new byte[4 + 128];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_CHAT).CopyTo(buffer, 2);

        Packet.WriteFixedString(buffer, 4, message, 128);
        client.Send(buffer);
    }

    public void SendRoomChat(string message)
    {
        if (lobbyRoomChatKind == LobbyRoomChatKind.None)
            return;

        byte[] buffer = new byte[4 + 128];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_CHAT).CopyTo(buffer, 2);

        Packet.WriteFixedString(buffer, 4, message, 128);
        client.Send(buffer);
    }

    public void SendFriendChat(uint targetPlayerId, string message)
    {
        if (targetPlayerId == 0 || targetPlayerId == myPlayerId)
            return;

        byte[] buffer = new byte[4 + 4 + 128];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_CHAT).CopyTo(buffer, 2);
        BitConverter.GetBytes(targetPlayerId).CopyTo(buffer, 4);

        Packet.WriteFixedString(buffer, 8, message, 128);
        client.Send(buffer);
    }

    public void SendFriendOverviewRequest()
    {
        SendHeaderOnly(PacketType.PKT_CS_FRIEND_OVERVIEW_REQUEST);
    }

    public void SendFriendPresenceSet(FriendPresenceMode mode)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_PRESENCE_SET)
            .CopyTo(buffer, 2);
        buffer[4] = (byte)mode;
        client.Send(buffer);
    }

    public void SendFriendSearch(string query)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[FRIEND SEARCH SEND BLOCKED] server is not connected");
            return;
        }

        if (!IsAuthenticated)
        {
            Debug.LogWarning("[FRIEND SEARCH SEND BLOCKED] login is not complete");
            return;
        }

        byte[] buffer = new byte[4 + 40];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_SEARCH)
            .CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, query, 40);
        Debug.Log($"[FRIEND SEARCH SEND] type=70 query='{query}' bytes={buffer.Length}");
        client.Send(buffer);
    }

    public void SendFriendRecommendRequest()
    {
        SendHeaderOnly(PacketType.PKT_CS_FRIEND_RECOMMEND_REQUEST);
    }

    public void SendFriendOperation(
        FriendOperation operation,
        string targetUserId)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            return;

        byte[] buffer = new byte[4 + 1 + 20];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_OPERATION)
            .CopyTo(buffer, 2);
        buffer[4] = (byte)operation;
        Packet.WriteFixedString(buffer, 5, targetUserId, 20);
        client.Send(buffer);
    }

    public void SendFriendRequestListRequest()
    {
        SendHeaderOnly(PacketType.PKT_CS_FRIEND_REQUEST_LIST_REQUEST);
    }

    public void SendFriendRecentRequest()
    {
        SendHeaderOnly(PacketType.PKT_CS_FRIEND_RECENT_REQUEST);
    }

    public void SendFriendMemoSet(string targetUserId, string memo)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            return;

        byte[] buffer = new byte[4 + 20 + 32];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_MEMO_SET)
            .CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId, 20);
        Packet.WriteFixedString(buffer, 24, memo ?? "", 32);
        client.Send(buffer);
    }

    public void SendProfileRequest(string targetUserId = "")
    {
        byte[] buffer = new byte[24];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PROFILE_REQUEST).CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId ?? string.Empty, 20);
        client.Send(buffer);
    }

    public void SendProfilePrivacySet(bool enabled)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PROFILE_PRIVACY_SET).CopyTo(buffer, 2);
        buffer[4] = enabled ? (byte)1 : (byte)0;
        client.Send(buffer);
    }

    public void SendProfileLike(string targetUserId)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            return;
        byte[] buffer = new byte[24];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PROFILE_LIKE).CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId, 20);
        client.Send(buffer);
    }
    public void SendProfileBattleStatsSet(IReadOnlyList<int> battleStatIds)
    {
        if (battleStatIds == null || battleStatIds.Count != 7) return;
        byte[] buffer = new byte[11];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PROFILE_BATTLE_STATS_SET).CopyTo(buffer, 2);
        for (int i = 0; i < 7; ++i)
        {
            if (battleStatIds[i] < 0 || battleStatIds[i] > 14) return;
            buffer[4 + i] = (byte)battleStatIds[i];
        }
        client.Send(buffer);
    }

    public void SendLobbyCharacterSelect(int modelId)
    {
        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_CHARACTER_SELECT).CopyTo(buffer, 2);
        BitConverter.GetBytes(modelId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    public void SendLobbyBackgroundSelect(int backgroundId)
    {
        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_BACKGROUND_SELECT).CopyTo(buffer, 2);
        BitConverter.GetBytes(backgroundId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    public void SendLobbyCharacterList()
    {

        byte[] bytes = new byte[4];

        BitConverter.GetBytes((ushort)bytes.Length).CopyTo(bytes, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_CHARACTER_LIST).CopyTo(bytes, 2);

        client.Send(bytes);
    }

    public void SendWeaponSkinLoadoutRequest(int characterId)
    {
        byte[] buffer = new byte[8];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_WEAPON_SKIN_LOADOUT_REQUEST).CopyTo(buffer, 2);
        BitConverter.GetBytes(characterId).CopyTo(buffer, 4);
        client.Send(buffer);
    }

    public void SendWeaponSkinSelect(int characterId, int weaponId, int skinId)
    {
        byte[] buffer = new byte[16];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_WEAPON_SKIN_SELECT).CopyTo(buffer, 2);
        BitConverter.GetBytes(characterId).CopyTo(buffer, 4);
        BitConverter.GetBytes(weaponId).CopyTo(buffer, 8);
        BitConverter.GetBytes(skinId).CopyTo(buffer, 12);
        client.Send(buffer);
    }

    public void SendCareerDetailRequest(
        string targetUserId,
        int seasonId,
        int modeType,
        int characterId)
    {
        byte[] buffer = new byte[33];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes(
            (ushort)PacketType.PKT_CS_CAREER_DETAIL_REQUEST
        ).CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId ?? string.Empty, 20);
        BitConverter.GetBytes(seasonId).CopyTo(buffer, 24);
        buffer[28] = (byte)Mathf.Clamp(modeType, 0, 2);
        BitConverter.GetBytes(Mathf.Clamp(characterId, 0, 3)).CopyTo(buffer, 29);
        client.Send(buffer);
    }

    public void SendChipCraftRequest(string targetUserId = "")
    {
        byte[] buffer = new byte[24];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_CHIPCRAFT_REQUEST).CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId ?? string.Empty, 20);
        client.Send(buffer);
    }

    public void SendLevelRewardRequest() => SendHeaderOnly(PacketType.PKT_CS_LEVEL_REWARD_REQUEST);
    public void SendEventPass(int operation, int index, long week)
    {
        byte[] buffer = new byte[20];
        BitConverter.GetBytes((ushort)20).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)334).CopyTo(buffer, 2);
        BitConverter.GetBytes(operation).CopyTo(buffer, 4);
        BitConverter.GetBytes(index).CopyTo(buffer, 8);
        BitConverter.GetBytes(week).CopyTo(buffer, 12);
        client.Send(buffer);
    }

    public void SendLevelRewardClaim(int targetLevel)
    {
        byte[] buffer = new byte[8];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LEVEL_REWARD_CLAIM).CopyTo(buffer, 2);
        BitConverter.GetBytes(targetLevel).CopyTo(buffer, 4);
        client.Send(buffer);
    }

    public void SendHonorRequest(int page = 1)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_HONOR_REQUEST).CopyTo(buffer, 2);
        buffer[4] = (byte)Mathf.Clamp(page, 1, 5);
        client.Send(buffer);
    }

    public void SendShopRequest(byte operation, int currency, int count, int itemId, ulong requestId, int page)
    {
        byte[] buffer = new byte[24];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)340).CopyTo(buffer, 2);
        buffer[4] = operation; buffer[5] = (byte)currency;
        BitConverter.GetBytes((ushort)count).CopyTo(buffer, 6);
        BitConverter.GetBytes(itemId).CopyTo(buffer, 8);
        BitConverter.GetBytes(requestId).CopyTo(buffer, 12);
        BitConverter.GetBytes(page).CopyTo(buffer, 20);
        client?.Send(buffer);
    }

    public void SendHonorClaim(int grade)
    {
        byte[] buffer = new byte[8];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_HONOR_CLAIM).CopyTo(buffer, 2);
        BitConverter.GetBytes(grade).CopyTo(buffer, 4);
        client.Send(buffer);
    }

    public void SendChipCraftSave(ChipCraftPresetData preset)
    {
        if (preset == null || preset.medals.Count > 3) return;
        byte[] buffer = new byte[61];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_CHIPCRAFT_SAVE).CopyTo(buffer, 2);
        WriteChipCraftPreset(buffer, 4, preset);
        client.Send(buffer);
    }

    private static void WriteChipCraftPreset(byte[] buffer, int offset, ChipCraftPresetData preset)
    {
        buffer[offset++] = (byte)preset.cardId;
        buffer[offset++] = (byte)preset.caseId;
        buffer[offset++] = (byte)preset.medals.Count;
        for (int i = 0; i < 3; ++i)
        {
            ChipCraftMedalData medal = i < preset.medals.Count ? preset.medals[i] : new ChipCraftMedalData();
            buffer[offset++] = (byte)medal.medalId;
            BitConverter.GetBytes(medal.x).CopyTo(buffer, offset); offset += 4;
            BitConverter.GetBytes(medal.y).CopyTo(buffer, offset); offset += 4;
            BitConverter.GetBytes(medal.rotation).CopyTo(buffer, offset); offset += 4;
            BitConverter.GetBytes(medal.scale).CopyTo(buffer, offset); offset += 4;
            buffer[offset++] = medal.flipped ? (byte)1 : (byte)0;
        }
    }

    private static ChipCraftPresetData ReadChipCraftPreset(byte[] body, ref int offset)
    {
        ChipCraftPresetData preset = new();
        preset.cardId = body[offset++];
        preset.caseId = body[offset++];
        int count = Mathf.Min(body[offset++], (byte)3);
        for (int i = 0; i < 3; ++i)
        {
            ChipCraftMedalData medal = new();
            medal.medalId = body[offset++];
            medal.x = BitConverter.ToSingle(body, offset); offset += 4;
            medal.y = BitConverter.ToSingle(body, offset); offset += 4;
            medal.rotation = BitConverter.ToSingle(body, offset); offset += 4;
            medal.scale = BitConverter.ToSingle(body, offset); offset += 4;
            medal.flipped = body[offset++] != 0;
            if (i < count) preset.medals.Add(medal);
        }
        return preset;
    }
    public void SendCareerDataRequests(int matchFilterType)
    {
        SendHeaderOnly(PacketType.PKT_CS_CAREER_OVERVIEW_REQUEST);
        SendCareerRankingRequest(0, 0, 1, string.Empty);
        SendCareerMatchHistoryRequest(matchFilterType);
        SendHeaderOnly(PacketType.PKT_CS_CAREER_INVENTORY_REQUEST);
    }

    public void SendCareerMatchHistoryRequest(int filterType)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes(
            (ushort)PacketType.PKT_CS_CAREER_MATCH_HISTORY_REQUEST
        ).CopyTo(buffer, 2);
        buffer[4] = (byte)Mathf.Clamp(filterType, 0, 2);
        client.Send(buffer);
    }

    public void SendCareerRankingRequest(
        int rankingType,
        int characterId,
        int page,
        string searchQuery)
    {
        const int QuerySize = 64;
        byte[] buffer = new byte[4 + 1 + 4 + 1 + QuerySize];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes(
            (ushort)PacketType.PKT_CS_CAREER_RANKING_REQUEST
        ).CopyTo(buffer, 2);
        buffer[4] = (byte)(rankingType == 1 ? 1 : 0);
        BitConverter.GetBytes(Math.Max(0, characterId)).CopyTo(buffer, 5);
        buffer[9] = (byte)Math.Clamp(page, 1, 255);

        string query = searchQuery ?? string.Empty;
        char[] queryChars = query.ToCharArray();
        byte[] queryBytes = new byte[QuerySize - 1];
        Encoding.UTF8.GetEncoder().Convert(
            queryChars,
            0,
            queryChars.Length,
            queryBytes,
            0,
            queryBytes.Length,
            true,
            out _,
            out int bytesUsed,
            out _
        );
        Array.Copy(queryBytes, 0, buffer, 10, bytesUsed);
        client.Send(buffer);
    }

    private void SendHeaderOnly(PacketType packetType)
    {
        byte[] buffer = new byte[4];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)packetType).CopyTo(buffer, 2);
        client.Send(buffer);
    }

    void HandlePacket(ushort type, byte[] body)
    {
        if (HandleReconnectPacket(type, body)) return;
        switch (type)
        {
            case (ushort)PacketType.PKT_SC_REGISTER_RESULT:
                HandleRegister(body);
                break;

            case (ushort)PacketType.PKT_SC_LOGIN_RESULT:
                HandleLogin(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_ENTER:
                HandleLobbyEnter(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_CHAT:
                HandleLobbyChat(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_CHAT:
                HandleRoomChat(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_CHAT:
                HandleFriendChat(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_OVERVIEW:
                HandleFriendOverview(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_PRESENCE_SET_RESULT:
                HandleFriendPresenceSetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_SEARCH:
                HandleFriendSearch(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_RECOMMEND:
                HandleFriendRecommend(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_OPERATION_RESULT:
                HandleFriendOperationResult(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_REQUEST_LIST:
                HandleFriendRequestList(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_RECENT:
                HandleFriendRecent(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_MEMO_SET_RESULT:
                HandleFriendMemoSetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_MAP_SET_RESULT:
                HandleRoomMapSetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PROFILE:
                HandleProfile(body);
                break;
            case (ushort)PacketType.PKT_SC_PROFILE_BIRTHDAY_SET_RESULT:
                HandleProfileBirthdayResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PROFILE_PRIVACY_SET_RESULT:
                HandleProfilePrivacySetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PROFILE_LIKE_RESULT:
                HandleProfileLikeResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PROFILE_BATTLE_STATS_SET_RESULT:
                HandleProfileBattleStatsSetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_CHIPCRAFT:
                HandleChipCraft(body);
                break;

            case (ushort)PacketType.PKT_SC_CHIPCRAFT_SAVE_RESULT:
                HandleChipCraftSaveResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LEVEL_REWARD_STATE:
                HandleLevelRewardState(body);
                break;

            case (ushort)PacketType.PKT_SC_LEVEL_REWARD_CLAIM_RESULT:
                HandleLevelRewardClaimResult(body);
                break;

            case (ushort)PacketType.PKT_SC_HONOR_STATE:
                HandleHonorState(body);
                break;

            case 335:
                FindFirstObjectByType<VectorEventPass>(FindObjectsInactive.Include)?.ApplyState(body);
                break;
            case 341: // Cosmetic shop response, handled by the main-thread packet queue.
                VectorShopPanel.Receive(body);
                break;

            case (ushort)PacketType.PKT_SC_HONOR_CLAIM_RESULT:
                HandleHonorClaimResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_CHARACTER_SELECT_RESULT:
                HandleLobbyCharacterSelectResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_BACKGROUND_SELECT_RESULT:
                HandleLobbyBackgroundSelectResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_CHARACTER_LIST:
                HandleLobbyCharacterList(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_SKIN_LOADOUT:
                HandleWeaponSkinLoadout(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_SKIN_SELECT_RESULT:
                HandleWeaponSkinSelectResult(body);
                break;

            case (ushort)PacketType.PKT_SC_CAREER_OVERVIEW:
                HandleCareerOverview(body);
                break;

            case (ushort)PacketType.PKT_SC_CAREER_DETAIL:
                HandleCareerDetail(body);
                break;
            case (ushort)PacketType.PKT_SC_CAREER_RANKING:
                HandleCareerRanking(body);
                break;

            case (ushort)PacketType.PKT_SC_CAREER_MATCH_HISTORY:
                HandleCareerMatchHistory(body);
                break;

            case (ushort)PacketType.PKT_SC_CAREER_INVENTORY:
                HandleCareerInventory(body);
                break;

            case (ushort)PacketType.PKT_SC_TEMP_GAME_ROOM_ENTER:
                HandleTempGameRoomEnter(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_APPEARANCE:
                HandleMatchAppearance(body);
                break;
            case (ushort)PacketType.PKT_SC_MATCH_READY_CARDS:
                HandleReadyCards(body);
                break;

            case (ushort)PacketType.PKT_SC_VOICE_RESPONSE:
                HandleVoiceResponse(body);
                break;

            case (ushort)PacketType.PKT_SC_PARTY_INVITE:
            case (ushort)PacketType.PKT_SC_SOCIAL_RESULT:
            case (ushort)PacketType.PKT_SC_PARTY_STATE:
            case (ushort)PacketType.PKT_SC_PARTY_AVAILABILITY:
            case (ushort)PacketType.PKT_SC_PARTY_MODE:
            case (ushort)PacketType.PKT_SC_MATCH_READY:
                HandleSocialPacket((PacketType)type, body);
                break;

            case (ushort)PacketType.PKT_SC_TEMP_GAME_START_RESULT:
                HandleTempGameStartResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_LIST:
                HandleRoomList(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_CREATE_RESULT:
                HandleRoomCreateResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_LEAVE_RESULT:
                HandleRoomLeaveResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_ENTER_RESULT:
                HandleRoomEnterResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_STATE:
                HandleRoomState(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_SLOT_CHANGE_RESULT:
                HandleRoomSlotChangeResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_SLOT_SWAP_REQUEST:
                HandleRoomSlotSwapRequest(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_TRANSFER_LEADER_RESULT:
                HandleRoomTransferLeaderResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_KICK_RESULT:
                HandleRoomKickResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_KICKED:
                HandleRoomKicked(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_GAME_START_RESULT:
                HandleRoomGameStartResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PICK_STATE:
                HandlePickState(body);
                break;

            case (ushort)PacketType.PKT_SC_PICK_LOCK_RESULT:
                HandlePickLockResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PICK_TIMER:
                HandlePickTimer(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_LOADING_INFO:
                HandleMatchLoadingInfo(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_START:
                HandleMatchStart(body);
                break;

            case (ushort)PacketType.PKT_SC_GAME_ALL_READY:
                HandleGameAllReady(body);
                break;

            case (ushort)PacketType.PKT_SC_GAME_PREPARE_TIMER:
                HandleGamePrepareTimer(body);
                break;

            case (ushort)PacketType.PKT_SC_MOVE_STATE:
                HandleMoveState(body);
                break;
            case 397:
                HandleMovementCorrection(body);
                break;

            case (ushort)PacketType.PKT_SC_HP_UPDATE:
                HandleHPUpdate(body);
                break;

            case (ushort)PacketType.PKT_SC_SHOT_CONFIRMED:
                HandleShotConfirmed(body);
                break;

            case (ushort)PacketType.PKT_SC_ROUND_RESULT:
                HandleRoundResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROUND_RESET:
                HandleRoundReset(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_RESULT:
                HandleMatchResult(body);
                break;

            case (ushort)PacketType.PKT_SC_AMMO_UPDATE:
                HandleAmmoUpdate(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_SWITCH_NOTIFY:
                HandleWeaponSwitchNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_ADS_STATE:
                HandleADSState(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_RESULT_DETAIL:
                HandleMatchResultDetail(body);
                break;

            case (ushort)PacketType.PKT_SC_RESULT_LEAVE_DONE:
                HandleResultLeaveDone(body);
                break;

            case (ushort)PacketType.PKT_SC_SCOREBOARD_UPDATE:
                HandleScoreboardUpdate(body);
                break;

            case (ushort)PacketType.PKT_SC_ROUND_TIMER:
                HandleRoundTimer(body);
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_PICKUP_NOTIFY:
                HandleSpikePickupNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_PLANTED:
                HandleSpikePlanted(body);
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_DROPPED:
                HandleSpikeDropped(body);
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_DEFUSE_STATE:
                HandleSpikeDefuseState(body);
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_DEFUSED:
                HandleSpikeDefused(body);
                break;

            case (ushort)PacketType.PKT_SC_DIE:
                HandleDie(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_DROPPED:
                HandleWeaponDropped(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_PICKUP_NOTIFY:
                HandleWeaponPickupNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_STORE_BUY_RESULT:
                HandleStoreBuyResult(body);
                break;

            case (ushort)PacketType.PKT_SC_SKILL_STATE:
                HandleSkillState(body);
                break;

            case (ushort)PacketType.PKT_SC_KNIFE_ATTACK_NOTIFY:
                HandleKnifeAttackNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_FIRE_NOTIFY:
                HandleFireNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_RELOAD_VISUAL:
                HandleReloadVisual(body);
                break;

            case (ushort)PacketType.PKT_SC_IN_GAME_CHAT:
                HandleInGameChat(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_SPAWN:
                HandleDroneSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_STATE:
                HandleDroneState(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_DESTROY:
                HandleDroneDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_TASER_FIRE:
                HandleDroneTaserFire(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_HP_UPDATE:
                HandleDroneHpUpdate(body);
                break;

            case (ushort)PacketType.PKT_SC_SCANNER_SPAWN:
                HandleScannerSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SCANNER_ANCHORED:
                HandleScannerAnchored(body);
                break;

            case (ushort)PacketType.PKT_SC_SCANNER_ZAP:
                HandleScannerZap(body);
                break;

            case (ushort)PacketType.PKT_SC_SCANNER_DESTROY:
                HandleScannerDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_EMP_CAST:
                HandleEmpCast(body);
                break;
            case (ushort)PacketType.PKT_SC_EMP_GESTURE:
                if(body!=null && body.Length==5 && body[4]<=1)
                    FindPlayerIdentityById(BitConverter.ToUInt32(body,0))?.GetComponent<InitiatorSkill_E>()?.ReceiveGesture(body[4]==1);
                break;
            case (ushort)PacketType.PKT_SC_SCANNER_GESTURE:
                if(body!=null && body.Length==5 && body[4]<=2)
                    FindPlayerIdentityById(BitConverter.ToUInt32(body,0))?.GetComponent<InitiatorSkill_C>()?.ReceiveGesture(body[4]);
                break;

            case (ushort)PacketType.PKT_SC_ELECTRIC_STATUS:
                HandleElectricStatus(body);
                break;

            case (ushort)PacketType.PKT_SC_DISCHARGE_GESTURE:
            case (ushort)PacketType.PKT_SC_DISCHARGE_BEGIN:
            case (ushort)PacketType.PKT_SC_DISCHARGE_STRIKE:
            case (ushort)PacketType.PKT_SC_DISCHARGE_END:
                DischargeDirector.Instance?.Receive(type, body);
                break;
            case (ushort)PacketType.PKT_SC_ULTIMATE_CAST:
                HandleUltimateCast(body);
                break;

            case (ushort)PacketType.PKT_SC_BEAST_STEALTH_STATE:
                HandleBeastStealthState(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_SMOKE_SPAWN:
                HandleEntrySmokeSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_SMOKE_ACTIVATE:
                HandleEntrySmokeActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_SMOKE_DESTROY:
                HandleEntrySmokeDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_SMOKE_STATE:
                HandleEntrySmokeState(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_DASH_CAST:
                HandleEntryDashCast(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_DASH_END:
                HandleEntryDashEnd(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_BLADE_STATE:
                HandleEntryBladeState(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_BLADE_FIRE:
                HandleEntryBladeFire(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SWAMP_SPAWN:
                HandleControllerSwampSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SWAMP_ACTIVATE:
                HandleControllerSwampActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SWAMP_DESTROY:
                HandleControllerSwampDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SMOKE_SPAWN:
                HandleControllerSmokeSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SMOKE_DESTROY:
                HandleControllerSmokeDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_DECOY_SPAWN:
                HandleControllerDecoySpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_DECOY_ACTIVATE:
                HandleControllerDecoyActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_DECOY_DESTROY:
                HandleControllerDecoyDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_DECOY_TRIGGER:
                if (body != null && body.Length == 8)
                    ControllerDecoyNetworkManager.Instance?.OnDecoyTrigger(BitConverter.ToUInt32(body, 0), BitConverter.ToSingle(body, 4));
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_ULTIMATE_SPAWN:
                HandleControllerUltimateSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_ULTIMATE_DESTROY:
                HandleControllerUltimateDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_C_SPAWN:
                HandleSentinelCSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_TRACK:
                SentinelCNetworkManager.Instance?.ReceiveTracking(body);
                break;
            case (ushort)PacketType.PKT_SC_SENTINEL_STATUS:
                SentinelCNetworkManager.Instance?.ReceiveStatus(body);
                break;
            case (ushort)PacketType.PKT_SC_SENTINEL_C_REVEAL:
                SentinelCNetworkManager.Instance?.ReceiveReveal(body);
                break;
            case (ushort)PacketType.PKT_SC_SENTINEL_GESTURE:
                if(body.Length==5 && body[4]<=2)FindPlayerIdentityById(BitConverter.ToUInt32(body,0))?.GetComponent<SentinelCastMotion>()?.ReceiveGesture(body[4]);
                break;
            case (ushort)PacketType.PKT_SC_SENTINEL_C_ANCHOR:
                HandleSentinelCAnchor(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_C_TRIGGER:
                HandleSentinelCTrigger(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_C_DESTROY:
                HandleSentinelCDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_Q_SPAWN:
                HandleSentinelQSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_Q_ACTIVATE:
                HandleSentinelQActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_Q_DESTROY:
                HandleSentinelQDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_E_SPAWN:
                HandleSentinelESpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_E_ACTIVATE:
                HandleSentinelEActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_E_DESTROY:
                HandleSentinelEDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_X_SPAWN:
                HandleSentinelXSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_X_DESTROY:
                HandleSentinelXDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_FAVOR_LEVEL_RESPONSE:    // 호감도 레벨
                HandleFavorLevelResponse(body);
                break;

            default:
                Debug.LogWarning($"처리 안 된 패킷 type={type}");
                break;


        }
    }

    void HandleRegister(byte[] body)
    {
        byte result = body[0];

        bool success = (result == (byte)RegisterResult.SUCCESS);

        Debug.Log($"[REGISTER RESULT] success={success}");

        loginUI?.OnRegisterResult(success);

        if (!success)
        {
            string msg = result switch
            {
                (byte)RegisterResult.ID_DUPLICATE => "아이디 중복",
                (byte)RegisterResult.NICK_DUPLICATE => "닉네임 중복",
                (byte)RegisterResult.PASSWORD_MISMATCH => "비밀번호 불일치",
                (byte)RegisterResult.INVALID_INPUT => "입력 오류",
                _ => "회원가입 실패"
            };

            loginUI?.ShowError(msg);
        }
    }

    void HandleLogin(byte[] body)
    {
        byte result = body[0];

        bool success = (result == (byte)LoginResult.SUCCESS);
        IsAuthenticated = success;

        if (ReconnectBusy) return; // Automatic reauthentication must not show Login UI.

        Debug.Log($"[LOGIN RESULT] success={success}");

        loginUI?.OnLoginResult(success);

        if (!success)
        {
            myUserId = "";

            string msg = result switch
            {
                (byte)LoginResult.NO_ID => "아이디 없음",
                (byte)LoginResult.WRONG_PASSWORD => "비밀번호 틀림",
                (byte)LoginResult.INVALID_INPUT => "입력 오류",
                (byte)LoginResult.ALREADY_LOGIN => "이미 로그인된 계정입니다.",
                _ => "로그인 실패"
            };

            loginUI?.ShowError(msg);
        }
    }
    void HandleLobbyEnter(byte[] body)
    {
        if (body == null || body.Length < 52)
        {
            Debug.LogWarning("[CLIENT LOBBY ENTER] invalid packet size");
            return;
        }

        string nickname = Packet.ReadFixedString(body, 0, 20);

        int level = BitConverter.ToInt32(body, 20);
        int currentExp = BitConverter.ToInt32(body, 24);
        int requiredExp = BitConverter.ToInt32(body, 28);

        int playerIconId = BitConverter.ToInt32(body, 32);
        int lobbyCharacterModelId = BitConverter.ToInt32(body, 36);
        int lobbyBackgroundId = BitConverter.ToInt32(body, 40);
        int freeCurrency = BitConverter.ToInt32(body, 44);
        int victoryCurrency = BitConverter.ToInt32(body, 48);
        if (body.Length >= 56)
            myPlayerId = BitConverter.ToUInt32(body, 52);

        Debug.Log($"[CLIENT LOBBY ENTER] nickname={nickname}, level={level}, exp={currentExp}/{requiredExp}, icon={playerIconId}, char={lobbyCharacterModelId}, bg={lobbyBackgroundId}, coin0={freeCurrency}, coin1={victoryCurrency}");

        MoneyManager.Instance?.SetServerBalances(
            freeCurrency,
            victoryCurrency
        );

        profilePanelController?.OnRecvLobbyEnter(
            nickname,
            level,
            currentExp,
            requiredExp,
            playerIconId,
            lobbyCharacterModelId,
            lobbyBackgroundId
        );

        SendWeaponSkinLoadoutRequest(lobbyCharacterModelId);

        SendFavorLevelRequest();        // 로비 씬 입장 마다 호감도 레벨 불러오기
    }

    private void HandleWeaponSkinLoadout(byte[] body)
    {
        if (body == null || body.Length < 4 + 2 + WeaponSkinWeaponCount * 8)
        {
            Debug.LogWarning("[WEAPON SKIN LOADOUT] invalid packet size");
            return;
        }

        int offset = 0;
        int characterId = BitConverter.ToInt32(body, offset);
        offset += 4;

        if (characterId < 0 || characterId >= WeaponSkinCharacterCount)
        {
            Debug.LogWarning($"[WEAPON SKIN LOADOUT] invalid characterId={characterId}");
            return;
        }

        ushort count = BitConverter.ToUInt16(body, offset);
        offset += 2;

        if (!equippedWeaponSkinsByCharacter.TryGetValue(
                characterId,
                out Dictionary<int, int> loadout))
        {
            loadout = new Dictionary<int, int>();
            equippedWeaponSkinsByCharacter[characterId] = loadout;
        }

        loadout.Clear();

        for (int i = 0; i < WeaponSkinWeaponCount; i++)
        {
            int weaponId = BitConverter.ToInt32(body, offset);
            offset += 4;
            int skinId = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (i < count && weaponId >= 0 && weaponId < WeaponSkinWeaponCount)
                loadout[weaponId] = skinId;
        }

        WeaponSkinLoadoutChanged?.Invoke(characterId);
        Debug.Log(
            $"[WEAPON SKIN LOADOUT] characterId={characterId}, received count={loadout.Count}"
        );
    }

    private void HandleWeaponSkinSelectResult(byte[] body)
    {
        if (body == null || body.Length < 13)
            return;

        bool success = body[0] != 0;
        int characterId = BitConverter.ToInt32(body, 1);
        int weaponId = BitConverter.ToInt32(body, 5);
        int skinId = BitConverter.ToInt32(body, 9);

        if (success)
        {
            if (!equippedWeaponSkinsByCharacter.TryGetValue(
                    characterId,
                    out Dictionary<int, int> loadout))
            {
                loadout = new Dictionary<int, int>();
                equippedWeaponSkinsByCharacter[characterId] = loadout;
            }

            loadout[weaponId] = skinId;
        }

        // On failure this reapplies the last confirmed value to the lobby UI.
        WeaponSkinLoadoutChanged?.Invoke(characterId);
        Debug.Log(
            $"[WEAPON SKIN SELECT] success={success}, characterId={characterId}, " +
            $"weaponId={weaponId}, skinId={skinId}"
        );
    }

    private void HandleProfile(byte[] body)
    {
        const int ExpectedSize = 711;
        if (body == null || body.Length < ExpectedSize)
        {
            Debug.LogWarning($"[PROFILE] invalid body size={body?.Length ?? 0}");
            return;
        }
        int offset = 0;
        if (body[offset++] != 0) { MyPagePanelController.Instance?.ShowLoadFailure(); return; }
        MyPageProfileData data = new();
        data.isSelf = body[offset++] != 0;
        data.privacyEnabled = body[offset++] != 0;
        data.userId = Packet.ReadFixedString(body, offset, 20); offset += 20;
        data.nickname = Packet.ReadFixedString(body, offset, 20); offset += 20;
        data.level = BitConverter.ToInt32(body, offset); offset += 4;
        data.playerIconId = BitConverter.ToInt32(body, offset); offset += 4;
        data.logoBorderId = BitConverter.ToInt32(body, offset); offset += 4;
        data.joinDate = Packet.ReadFixedString(body, offset, 16); offset += 16;
        data.region = Packet.ReadFixedString(body, offset, 20); offset += 20;
        data.birthday = Packet.ReadFixedString(body, offset, 16); offset += 16;
        data.likeCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.seasonId = BitConverter.ToInt32(body, offset); offset += 4;
        data.currentRankImageId = BitConverter.ToInt32(body, offset); offset += 4;
        data.currentTierName = Packet.ReadFixedString(body, offset, 32); offset += 32;
        data.seasonHighestRankImageId = BitConverter.ToInt32(body, offset); offset += 4;
        data.allTimeHighestRankImageId = BitConverter.ToInt32(body, offset); offset += 4;
        data.allTimeHighestSeasonId = BitConverter.ToInt32(body, offset); offset += 4;
        data.playCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.wins = BitConverter.ToInt32(body, offset); offset += 4;
        data.roundsPlayed = BitConverter.ToInt32(body, offset); offset += 4;
        data.totalKills = BitConverter.ToInt32(body, offset); offset += 4;
        data.totalAssists = BitConverter.ToInt32(body, offset); offset += 4;
        data.totalDamage = BitConverter.ToInt32(body, offset); offset += 4;
        data.mvpCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.headshotKills = BitConverter.ToInt32(body, offset); offset += 4;
        data.spikePlants = BitConverter.ToInt32(body, offset); offset += 4;
        data.spikeDefuses = BitConverter.ToInt32(body, offset); offset += 4;
        data.weaponSkinOwnedCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.weaponSkinTotalCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.characterSkinOwnedCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.characterSkinTotalCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.battleStatIds = new int[7];
        for (int i = 0; i < data.battleStatIds.Length; ++i)
            data.battleStatIds[i] = body[offset++];
        for (int i = 0; i < 4; ++i)
        {
            MyPageCharacterData character = new();
            character.characterId = BitConverter.ToInt32(body, offset); offset += 4;
            character.matches = BitConverter.ToInt32(body, offset); offset += 4;
            character.wins = BitConverter.ToInt32(body, offset); offset += 4;
            character.maxKills = BitConverter.ToInt32(body, offset); offset += 4;
            character.owned = body[offset++] != 0;
            data.characters.Add(character);
        }
        int visitorCount = Mathf.Min(body[offset++], (byte)8);
        for (int i = 0; i < 8; ++i)
        {
            MyPageVisitorData visitor = new();
            visitor.userId = Packet.ReadFixedString(body, offset, 20); offset += 20;
            visitor.nickname = Packet.ReadFixedString(body, offset, 20); offset += 20;
            visitor.playerIconId = BitConverter.ToInt32(body, offset); offset += 4;
            visitor.visitedAtUnix = BitConverter.ToInt64(body, offset); offset += 8;
            if (i < visitorCount) data.visitors.Add(visitor);
        }
        MyPagePanelController.Instance?.ApplyProfile(data);
    }

    private void HandleProfilePrivacySetResult(byte[] body)
    {
        if (body == null || body.Length < 2) return;
        MyPagePanelController.Instance?.ApplyPrivacyResult(body[0] == 0, body[1] != 0);
    }

    private void HandleProfileLikeResult(byte[] body)
    {
        if (body == null || body.Length < 25) return;
        bool success = body[0] == 0;
        string targetUserId = Packet.ReadFixedString(body, 1, 20);
        int likeCount = BitConverter.ToInt32(body, 21);
        MyPagePanelController.Instance?.ApplyLikeResult(success, targetUserId, likeCount);
    }
    private void HandleProfileBattleStatsSetResult(byte[] body)
    {
        if (body == null || body.Length < 8) return;
        int[] battleStatIds = new int[7];
        for (int i = 0; i < battleStatIds.Length; ++i) battleStatIds[i] = body[1 + i];
        MyPagePanelController.Instance?.ApplyBattleStatsResult(body[0] == 0, battleStatIds);
    }

    private void HandleCareerOverview(byte[] body)
    {
        const int ExpectedBodySize = 124;
        if (body == null || body.Length < ExpectedBodySize)
        {
            Debug.LogWarning("[CAREER OVERVIEW] invalid packet size");
            return;
        }

        int offset = 0;
        CareerOverviewData data = new();
        data.seasonId = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.seasonName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;
        data.seasonEndUnix = BitConverter.ToInt64(body, offset);
        offset += 8;
        data.remainingSeasonDays = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.tierId = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.tierName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;
        data.rankImageId = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.tierMinPoints = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.tierMaxPoints = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.rankPoints = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.rankedMatches = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.wins = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.roundsPlayed = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.kills = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.totalDamage = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.mvpCount = BitConverter.ToInt32(body, offset);

        CareerPanelController panel = FindFirstObjectByType<CareerPanelController>();
        panel?.ApplyOverview(data);
    }

    private void HandleCareerDetail(byte[] body)
    {
        const int ExpectedBodySize = 492;
        const int SeasonEntrySize = 37;
        const int SeasonCapacity = 8;
        const int CharacterCapacity = 4;
        if (body == null || body.Length < ExpectedBodySize)
        {
            Debug.LogWarning("[CAREER DETAIL] invalid packet size");
            return;
        }

        int offset = 0;
        CareerDetailData data = new();
        data.success = body[offset++] == 0;
        data.hasData = body[offset++] != 0;
        data.seasonId = BitConverter.ToInt32(body, offset); offset += 4;
        data.modeType = body[offset++];
        data.selectedCharacterId = BitConverter.ToInt32(body, offset); offset += 4;
        int seasonCount = Math.Min((int)body[offset++], SeasonCapacity);

        for (int i = 0; i < SeasonCapacity; ++i)
        {
            CareerDetailSeasonData season = new();
            season.seasonId = BitConverter.ToInt32(body, offset); offset += 4;
            season.seasonName = Packet.ReadFixedString(body, offset, 32); offset += 32;
            season.isActive = body[offset++] != 0;
            if (i < seasonCount) data.seasons.Add(season);
        }

        for (int i = 0; i < CharacterCapacity; ++i)
        {
            CareerDetailCharacterData character = new();
            character.characterId = BitConverter.ToInt32(body, offset); offset += 4;
            character.performanceScore = BitConverter.ToInt32(body, offset); offset += 4;
            character.matches = BitConverter.ToInt32(body, offset); offset += 4;
            character.wins = BitConverter.ToInt32(body, offset); offset += 4;
            character.kills = BitConverter.ToInt32(body, offset); offset += 4;
            character.deaths = BitConverter.ToInt32(body, offset); offset += 4;
            character.maxKills = BitConverter.ToInt32(body, offset); offset += 4;
            character.maxDamage = BitConverter.ToInt32(body, offset); offset += 4;
            data.characters.Add(character);
        }

        data.playCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.wins = BitConverter.ToInt32(body, offset); offset += 4;
        data.roundsPlayed = BitConverter.ToInt32(body, offset); offset += 4;
        data.totalDamage = BitConverter.ToInt32(body, offset); offset += 4;
        data.kills = BitConverter.ToInt32(body, offset); offset += 4;
        data.deaths = BitConverter.ToInt32(body, offset); offset += 4;
        data.assists = BitConverter.ToInt32(body, offset); offset += 4;
        data.headshotKills = BitConverter.ToInt32(body, offset); offset += 4;
        data.meleeKills = BitConverter.ToInt32(body, offset); offset += 4;
        data.spikePlants = BitConverter.ToInt32(body, offset); offset += 4;
        data.spikeDefuses = BitConverter.ToInt32(body, offset); offset += 4;
        data.maxAssists = BitConverter.ToInt32(body, offset); offset += 4;
        data.maxRoundDamageHundredths = BitConverter.ToInt32(body, offset); offset += 4;
        data.maxRoundKillsHundredths = BitConverter.ToInt32(body, offset);

        MyPageCareerDataController panel =
            FindFirstObjectByType<MyPageCareerDataController>(FindObjectsInactive.Include);
        panel?.ApplyData(data);
    }

    private void HandleChipCraft(byte[] body)
    {
        if (body == null || body.Length < 82 || body[0] != 0) return;
        int offset = 1;
        ChipCraftData data = new();
        data.isSelf = body[offset++] != 0;
        data.userId = Packet.ReadFixedString(body, offset, 20); offset += 20;
        data.ownedCardMask = body[offset++];
        data.ownedCaseMask = body[offset++];
        data.ownedMedalMask = body[offset++];
        data.preset = ReadChipCraftPreset(body, ref offset);
        MyPagePanelController.Instance?.ApplyChipCraft(data);
    }

    private void HandleChipCraftSaveResult(byte[] body)
    {
        if (body == null || body.Length < 58) return;
        int offset = 1;
        ChipCraftPresetData preset = body[0] == 0 ? ReadChipCraftPreset(body, ref offset) : null;
        // Apply the authoritative appearance even if the profile page was closed
        // or switched to another user while the save response was in flight.
        if (preset != null)
        {
            FindFirstObjectByType<ProfilePanelController>(FindObjectsInactive.Include)?.ApplyPlayerIcon(preset.cardId);
            FindFirstObjectByType<GamePanelController>(FindObjectsInactive.Include)?.RefreshLocalChipCraft(preset);
        }
        MyPagePanelController.Instance?.ApplyChipCraftSaveResult(body[0] == 0, preset);
    }

    private void HandleLevelRewardState(byte[] body)
    {
        if (body == null || body.Length < 25) return;
        MyPageLevelRewardController panel = FindFirstObjectByType<MyPageLevelRewardController>(FindObjectsInactive.Include);
        panel?.ApplyState(body[0] == 0,
            BitConverter.ToInt32(body, 1), BitConverter.ToInt32(body, 5),
            BitConverter.ToInt32(body, 9), BitConverter.ToInt32(body, 13));
        if (body[0] == 0)
            MoneyManager.Instance?.SetServerBalances(BitConverter.ToInt32(body, 17), BitConverter.ToInt32(body, 21));
    }

    private void HandleLevelRewardClaimResult(byte[] body)
    {
        if (body == null || body.Length < 21) return;
        MyPageLevelRewardController panel = FindFirstObjectByType<MyPageLevelRewardController>(FindObjectsInactive.Include);
        panel?.ApplyClaim(body[0] == 0, BitConverter.ToInt32(body, 1),
            BitConverter.ToInt32(body, 5), BitConverter.ToInt32(body, 9));
        if (body[0] == 0)
            MoneyManager.Instance?.SetServerBalances(BitConverter.ToInt32(body, 13), BitConverter.ToInt32(body, 17));
    }

    private void HandleHonorState(byte[] body)
    {
        if (body == null || body.Length < 20) return;
        int offset = 0;
        bool success = body[offset++] == 0;
        int score = BitConverter.ToInt32(body, offset); offset += 4;
        int claimed = BitConverter.ToInt32(body, offset); offset += 4;
        int free = BitConverter.ToInt32(body, offset); offset += 4;
        int victory = BitConverter.ToInt32(body, offset); offset += 4;
        int page = body[offset++], pageCount = body[offset++], count = body[offset++];
        if (count > 10 || body.Length < 20 + count * 13) return;
        var changes = new System.Collections.Generic.List<HonorChangeData>(count);
        for (int index = 0; index < count; ++index)
        {
            long time = BitConverter.ToInt64(body, offset); offset += 8;
            int delta = BitConverter.ToInt32(body, offset); offset += 4;
            byte reason = body[offset++];
            changes.Add(new HonorChangeData(time, delta, reason));
        }
        FindFirstObjectByType<MyPageHonorController>(FindObjectsInactive.Include)
            ?.ApplyState(success, score, claimed, page, pageCount, changes);
        if (success) MoneyManager.Instance?.SetServerBalances(free, victory);
    }

    private void HandleHonorClaimResult(byte[] body)
    {
        if (body == null || body.Length < 21) return;
        bool success = body[0] == 0;
        int grade = BitConverter.ToInt32(body, 1);
        int freeAward = BitConverter.ToInt32(body, 5);
        int victoryAward = BitConverter.ToInt32(body, 9);
        FindFirstObjectByType<MyPageHonorController>(FindObjectsInactive.Include)
            ?.ApplyClaim(success, grade, freeAward, victoryAward);
        if (success)
            MoneyManager.Instance?.SetServerBalances(
                BitConverter.ToInt32(body, 13), BitConverter.ToInt32(body, 17));
    }
    private void HandleCareerRanking(byte[] body)
    {
        const int EntrySize = 100;
        const int HeaderSize = 12;
        const int MaxEntries = 10;
        if (body == null || body.Length < HeaderSize)
            return;

        int offset = 0;
        CareerRankingPageData pageData = new();
        pageData.rankingType = body[offset++];
        pageData.characterId = BitConverter.ToInt32(body, offset);
        offset += 4;
        pageData.page = body[offset++];
        pageData.totalPages = Math.Max(1, (int)body[offset++]);
        pageData.totalEntries = BitConverter.ToInt32(body, offset);
        offset += 4;
        int count = Math.Min((int)body[offset++], MaxEntries);

        if (body.Length < HeaderSize + count * EntrySize)
        {
            Debug.LogWarning("[CAREER RANKING] invalid packet size");
            return;
        }

        for (int i = 0; i < count; ++i)
        {
            CareerRankingEntryData entry = new();
            entry.position = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.userId = Packet.ReadFixedString(body, offset, 20);
            offset += 20;
            entry.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;
            entry.playerIconId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.tierId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.tierName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;
            entry.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.rankPoints = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.rankedMatches = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.wins = BitConverter.ToInt32(body, offset);
            offset += 4;
            pageData.entries.Add(entry);
        }

        CareerPanelController panel = FindFirstObjectByType<CareerPanelController>();
        panel?.ApplyRanking(pageData);
    }

    private void HandleCareerMatchHistory(byte[] body)
    {
        const int PlayerEntrySize = 78;
        const int PlayersPerMatch = 6;
        const int MatchEntrySize = 57 + PlayerEntrySize * PlayersPerMatch;
        const int MaxMatches = 5;

        if (body == null || body.Length < 2)
            return;

        int filterType = Math.Min((int)body[0], 2);
        int count = Math.Min((int)body[1], MaxMatches);
        if (body.Length < 2 + count * MatchEntrySize)
        {
            Debug.LogWarning("[CAREER MATCH HISTORY] invalid packet size");
            return;
        }

        int offset = 2;
        List<CareerMatchData> matches = new(count);

        for (int i = 0; i < count; ++i)
        {
            CareerMatchData match = new();
            match.matchId = BitConverter.ToUInt64(body, offset);
            offset += 8;
            match.mapName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;
            match.startedAtUnix = BitConverter.ToInt64(body, offset);
            offset += 8;
            match.durationSeconds = BitConverter.ToInt32(body, offset);
            offset += 4;
            match.scoreA = body[offset++];
            match.scoreB = body[offset++];
            match.winnerTeam = body[offset++];
            match.myTeam = body[offset++];
            int playerCount = Math.Min((int)body[offset++], PlayersPerMatch);

            for (int playerIndex = 0; playerIndex < PlayersPerMatch; ++playerIndex)
            {
                CareerMatchPlayerData player = new();
                player.userId = Packet.ReadFixedString(body, offset, 20);
                offset += 20;
                player.nickname = Packet.ReadFixedString(body, offset, 20);
                offset += 20;
                player.team = body[offset++];
                player.characterId = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.combatScore = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.attackScore = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.skillScore = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.teamworkScore = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.kills = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.deaths = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.assists = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.damage = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.isMvp = body[offset++] != 0;

                if (playerIndex < playerCount)
                    match.players.Add(player);
            }

            matches.Add(match);
        }

        CareerPanelController panel = FindFirstObjectByType<CareerPanelController>();
        panel?.ApplyMatchHistory(filterType, matches);
    }

    private void HandleCareerInventory(byte[] body)
    {
        const int EntrySize = 148;
        const int MaxEntries = 20;
        if (body == null || body.Length < 1)
            return;

        int count = Math.Min((int)body[0], MaxEntries);
        if (body.Length < 1 + count * EntrySize)
        {
            Debug.LogWarning("[CAREER INVENTORY] invalid packet size");
            return;
        }

        int offset = 1;
        List<CareerInventoryEntryData> entries = new(count);

        for (int i = 0; i < count; ++i)
        {
            CareerInventoryEntryData entry = new();
            entry.itemId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.itemName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;
            entry.description = Packet.ReadFixedString(body, offset, 96);
            offset += 96;
            entry.iconId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.quantity = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.acquiredAtUnix = BitConverter.ToInt64(body, offset);
            offset += 8;
            entries.Add(entry);
        }

        CareerPanelController panel = FindFirstObjectByType<CareerPanelController>();
        panel?.ApplyInventory(entries);
    }

    void HandleLobbyChat(byte[] body)
    {
        Debug.Log($"[CLIENT HANDLE LOBBY CHAT] bodyLen={body.Length}");

        uint playerId = BitConverter.ToUInt32(body, 0);
        string nickname = Packet.ReadFixedString(body, 4, 20);
        string message = Packet.ReadFixedString(body, 24, 128);

        Debug.Log($"[CLIENT CHAT PARSED] playerId={playerId} nickname={nickname} message={message}");

        chatPanelController?.OnRecvLobbyChat(playerId, nickname, message);
    }

    void HandleRoomChat(byte[] body)
    {
        if (body == null || body.Length < 156)
            return;

        uint roomId = BitConverter.ToUInt32(body, 0);
        uint playerId = BitConverter.ToUInt32(body, 4);
        string nickname = Packet.ReadFixedString(body, 8, 20);
        string message = Packet.ReadFixedString(body, 28, 128);

        chatPanelController?.OnRecvRoomChat(
            roomId,
            playerId,
            nickname,
            message
        );
    }

    void HandleFriendChat(byte[] body)
    {
        if (body == null || body.Length < 156)
            return;

        uint senderPlayerId = BitConverter.ToUInt32(body, 0);
        uint targetPlayerId = BitConverter.ToUInt32(body, 4);
        string senderNickname = Packet.ReadFixedString(body, 8, 20);
        string message = Packet.ReadFixedString(body, 28, 128);

        chatPanelController?.OnRecvFriendChat(
            senderPlayerId,
            targetPlayerId,
            senderNickname,
            message
        );
    }

    private void HandleFriendOverview(byte[] body)
    {
        const int UserSize = 89;
        const int MaxContacts = 50;
        if (body == null || body.Length < 3 + MaxContacts * UserSize * 2)
            return;

        FriendPresenceMode presenceMode = body[0] == 1
            ? FriendPresenceMode.DoNotDisturb
            : FriendPresenceMode.Online;
        int friendCount = Math.Min((int)body[1], MaxContacts);
        int blockedCount = Math.Min((int)body[2], MaxContacts);
        int offset = 3;

        List<FriendUserData> friends = new(friendCount);
        for (int i = 0; i < MaxContacts; i++)
        {
            FriendUserData user = ReadFriendUser(body, ref offset);
            if (i < friendCount)
                friends.Add(user);
        }

        List<FriendUserData> blocked = new(blockedCount);
        for (int i = 0; i < MaxContacts; i++)
        {
            FriendUserData user = ReadFriendUser(body, ref offset);
            if (i < blockedCount)
                blocked.Add(user);
        }

        friendPanelController?.ApplyOverview(presenceMode, friends, blocked);
        chatPanelController?.ApplyFriendOverview(friends);
    }

    private void HandleFriendPresenceSetResult(byte[] body)
    {
        if (body == null || body.Length < 2)
            return;

        FriendPresenceMode mode = body[1] == 1
            ? FriendPresenceMode.DoNotDisturb
            : FriendPresenceMode.Online;
        friendPanelController?.ApplyPresenceResult(body[0] == 0, mode);
    }

    private void HandleFriendSearch(byte[] body)
    {
        List<FriendUserData> users = ReadFriendUserList(body, 10);
        Debug.Log(
            $"[FRIEND SEARCH RECV] type=71 count={users.Count} bodyBytes={body?.Length ?? 0}"
        );

        if (friendPanelController == null)
        {
            Debug.LogWarning("[FRIEND SEARCH RECV] FriendPanelController is not registered");
            return;
        }

        friendPanelController.ApplySearchResults(users);
    }

    private void HandleFriendRecommend(byte[] body)
    {
        friendPanelController?.ApplyRecommendations(
            ReadFriendUserList(body, 3)
        );
    }

    private void HandleFriendOperationResult(byte[] body)
    {
        if (body == null || body.Length < 22)
            return;

        FriendOperation operation = (FriendOperation)body[0];
        FriendOperationResult result = (FriendOperationResult)body[1];
        string targetUserId = Packet.ReadFixedString(body, 2, 20);
        friendPanelController?.ApplyOperationResult(
            operation,
            result,
            targetUserId
        );
    }

    private void HandleFriendRequestList(byte[] body)
    {
        const int MaxRequests = 30;
        const int RequestSize = 90;
        if (body == null || body.Length < 1 + MaxRequests * RequestSize)
            return;

        int count = Math.Min((int)body[0], MaxRequests);
        int offset = 1;
        List<FriendRequestData> requests = new(count);

        for (int i = 0; i < MaxRequests; i++)
        {
            bool incoming = body[offset++] == 1;
            FriendUserData user = ReadFriendUser(body, ref offset);
            if (i < count)
            {
                requests.Add(new FriendRequestData
                {
                    incoming = incoming,
                    user = user
                });
            }
        }

        friendPanelController?.ApplyRequests(requests);
    }

    private void HandleFriendRecent(byte[] body)
    {
        friendPanelController?.ApplyRecent(
            ReadFriendUserList(body, 20)
        );
    }

    private void HandleFriendMemoSetResult(byte[] body)
    {
        if (body == null || body.Length < 53)
            return;

        bool success = body[0] == 0;
        string targetUserId = Packet.ReadFixedString(body, 1, 20);
        string memo = Packet.ReadFixedString(body, 21, 32);
        Debug.Log($"[FRIEND MEMO RESULT] success={success} target={targetUserId} memo='{memo}'");

        if (success)
            SendFriendOverviewRequest();
    }

    private static List<FriendUserData> ReadFriendUserList(
        byte[] body,
        int maxCount)
    {
        const int UserSize = 89;
        List<FriendUserData> users = new();
        if (body == null || body.Length < 1 + maxCount * UserSize)
            return users;

        int count = Math.Min((int)body[0], maxCount);
        int offset = 1;
        for (int i = 0; i < maxCount; i++)
        {
            FriendUserData user = ReadFriendUser(body, ref offset);
            if (i < count)
                users.Add(user);
        }
        return users;
    }

    private static FriendUserData ReadFriendUser(byte[] body, ref int offset)
    {
        FriendUserData user = new();
        user.playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;
        user.userId = Packet.ReadFixedString(body, offset, 20);
        offset += 20;
        user.nickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;
        user.level = BitConverter.ToInt32(body, offset);
        offset += 4;
        user.playerIconId = BitConverter.ToInt32(body, offset);
        offset += 4;
        user.rankImageId = BitConverter.ToInt32(body, offset);
        offset += 4;
        user.presenceStatus = (FriendPresenceStatus)body[offset++];
        user.memo = Packet.ReadFixedString(body, offset, 32);
        offset += 32;
        return user;
    }

    void HandleLobbyCharacterSelectResult(byte[] body)
    {

        byte result = body[0];
        int modelId = BitConverter.ToInt32(body, 1);


        lobbyPanelController?.OnRecvLobbyCharacterSelectResult(result, modelId);
    }

    void HandleLobbyBackgroundSelectResult(byte[] body)
    {
        byte result = body[0];
        int backgroundId = BitConverter.ToInt32(body, 1);

        lobbyPanelController?.OnRecvLobbyBackgroundSelectResult(result, backgroundId);
    }

    void HandleLobbyCharacterList(byte[] body)
    {
        ushort count = BitConverter.ToUInt16(body, 0);
        int currentCharacterModelId = BitConverter.ToInt32(body, 2);

        List<LobbyCharacterData> list = new List<LobbyCharacterData>();

        int offset = 6;
        for (int i = 0; i < count; i++)
        {
            LobbyCharacterData data = new LobbyCharacterData();
            data.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            data.owned = body[offset] != 0;
            offset += 1;

            list.Add(data);
        }

        lobbyPanelController?.OnRecvLobbyCharacterList(list, currentCharacterModelId);
    }

    public void SendTempGameRoomEnter()
    {
        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_TEMP_GAME_ROOM_ENTER).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    public void SendTempGameStart(int mode)
    {
        byte[] buffer = new byte[5];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_TEMP_GAME_START).CopyTo(buffer, 2);
        buffer[4] = (byte)Mathf.Clamp(mode, 0, 1);

        client.Send(buffer);
    }

    public void SendMatchmakingCancel()
    {
        SendHeaderOnly(PacketType.PKT_CS_MATCHMAKING_CANCEL);
    }

    void HandleTempGameRoomEnter(byte[] body)
    {
        const int BaseSlotSize = 33;
        const int PresetSize = 57;
        if (body == null || body.Length < 2 + 3 * BaseSlotSize) return;
        bool hasPresets = body.Length >= 2 + 3 * (BaseSlotSize + PresetSize);
        byte result = body[0];
        byte playerCount = body[1];
        if (playerCount > 3) return;

        if (result != 0)
        {
            Debug.LogWarning("[TEMP GAME ROOM] enter fail");
            return;
        }

        List<TempGamePlayerData> players = new List<TempGamePlayerData>();

        int offset = 2;
        // Unused fixed-capacity slots are zero-filled (including slotIndex = 0).
        // They must never overwrite the occupied leader card with an empty player.
        for (int i = 0; i < playerCount; i++)
        {
            TempGamePlayerData data = new TempGamePlayerData();
            data.slotIndex = body[offset];
            offset += 1;

            data.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            data.playerIconId = BitConverter.ToInt32(body, offset);
            offset += 4;

            data.logoBorderId = BitConverter.ToInt32(body, offset);
            offset += 4;

            data.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (hasPresets)
                data.preset = ReadChipCraftPreset(body, ref offset);

            VectorVoiceChat.Instance?.ObserveProfile(data.nickname, data.playerIconId);
            players.Add(data);
        }

        SetReadyRoomChatContext();
        gamePanelController?.OnRecvTempGameRoomEnter(playerCount, players);
    }

    void HandleTempGameStartResult(byte[] body)
    {
        if (body == null || body.Length < 10)
            return;

        byte result = body[0];
        byte state = body[1];
        byte mode = body[2];
        ushort elapsedSeconds = BitConverter.ToUInt16(body, 3);
        byte tierRange = body[5];
        uint roomId = BitConverter.ToUInt32(body, 6);
        gamePanelController?.OnRecvTempGameStartResult(
            result,
            state,
            mode,
            elapsedSeconds,
            tierRange,
            roomId);
    }

    public void SendRoomList()
    {
        Debug.Log("[CLIENT] SendRoomList");

        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_LIST).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    void HandleRoomList(byte[] body)
    {
        Debug.Log($"[ROOMLIST] bodyLength={body.Length}");

        int offset = 0;

        ushort roomCount = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log($"[ROOMLIST] roomCount={roomCount}");

        List<RoomListData> rooms = new();

        for (int i = 0; i < roomCount; i++)
        {
            Debug.Log($"[ROOM PARSE START] index={i} offset={offset}");

            RoomListData room = new RoomListData();

            room.roomId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            // 문자열 raw 바이트 확인
            Debug.Log($"[RAW roomName bytes] {BitConverter.ToString(body, offset, 32)}");
            room.roomName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;

            Debug.Log($"[RAW mapName bytes] {BitConverter.ToString(body, offset, 32)}");
            room.mapName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;

            Debug.Log($"[RAW hostName bytes] {BitConverter.ToString(body, offset, 20)}");
            room.hostName = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            room.currentPlayerCount = BitConverter.ToUInt16(body, offset);
            offset += 2;

            room.maxPlayerCount = BitConverter.ToUInt16(body, offset);
            offset += 2;

            room.state = body[offset];
            offset += 1;

            Debug.Log($"[ROOM PARSED] id={room.roomId}, name={room.roomName}, map={room.mapName}, host={room.hostName}");

            rooms.Add(room);
        }

        LobbyManager.Instance.Game.OnRecvRoomList(rooms);
    }

    public void SendRoomCreate()
    {
        Debug.Log("[CLIENT] SendRoomCreate");

        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_CREATE).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    public void SendRoomEnter(uint roomId)
    {
        Debug.Log($"[CLIENT] SendRoomEnter roomId={roomId}");

        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_ENTER).CopyTo(buffer, 2);
        BitConverter.GetBytes(roomId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    void HandleRoomCreateResult(byte[] body)
    {
        Debug.Log($"[ROOM CREATE RESULT] bodyLength={body.Length}");

        int offset = 0;

        byte result = body[offset];
        offset += 1;

        if (result != 0)
        {
            Debug.LogWarning($"[ROOM CREATE FAIL] result={result}");
            return;
        }

        RoomStateData roomState = new RoomStateData();

        roomState.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        roomState.roomName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        roomState.mapName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        roomState.isLeader = body[offset] != 0;
        offset += 1;

        roomState.canStart = body[offset] != 0;
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < memberCount; i++)
        {
            RoomMemberData member = new RoomMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.level = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.playerIconId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.team = (RoomSlotTeam)body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.isLeader = body[offset] != 0;
            offset += 1;

            roomState.members.Add(member);
        }

        SetPrivateRoomChatContext(roomState.roomId, roomState.roomName);
        gamePanelController?.OnRecvRoomCreateResult(roomState);
    }

    public void SendRoomLeave()
    {
        Debug.Log("[CLIENT] SendRoomLeave");

        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_LEAVE).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    void HandleRoomLeaveResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        uint roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte roomDeleted = body[offset];
        offset += 1;

        Debug.Log($"[ROOM LEAVE RESULT] result={result}, roomId={roomId}, roomDeleted={roomDeleted}");

        if (result == 0)
            ClearPrivateRoomChatContext(roomId);

        gamePanelController?.OnRecvRoomLeaveResult(result, roomId, roomDeleted != 0);
    }

    void HandleRoomEnterResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        uint roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        Debug.Log($"[ROOM ENTER RESULT] result={result}, roomId={roomId}");

        gamePanelController?.OnRecvRoomEnterResult(result, roomId);
    }

    void HandleRoomState(byte[] body)
    {
        Debug.Log($"[ROOM STATE] bodyLength={body.Length}");

        int offset = 0;

        RoomStateData roomState = new RoomStateData();

        roomState.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        roomState.roomName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        roomState.mapName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        roomState.canStart = body[offset] != 0;
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        Debug.Log(
            $"[ROOM STATE PARSED] " +
            $"roomId={roomState.roomId}, " +
            $"roomName={roomState.roomName}, " +
            $"mapName={roomState.mapName}, " +
            $"canStart={roomState.canStart}, " +
            $"memberCount={memberCount}"
        );

        for (int i = 0; i < memberCount; i++)
        {
            RoomMemberData member = new RoomMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.level = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.playerIconId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.team = (RoomSlotTeam)body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.isLeader = body[offset] != 0;
            offset += 1;

            roomState.members.Add(member);

            Debug.Log(
                $"[ROOM STATE MEMBER] " +
                $"playerId={member.playerId}, " +
                $"nickname={member.nickname}, " +
                $"level={member.level}, " +
                $"team={member.team}, " +
                $"slot={member.slotIndex}, " +
                $"leader={member.isLeader}"
            );
        }

        SetPrivateRoomChatContext(roomState.roomId, roomState.roomName);
        gamePanelController?.OnRecvRoomState(roomState);
    }

    public void SendRoomSlotChange(RoomSlotTeam targetTeam, byte targetSlotIndex)
    {
        Debug.Log($"[CLIENT] SendRoomSlotChange team={targetTeam}, slot={targetSlotIndex}");

        byte[] buffer = new byte[4 + 1 + 1];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_SLOT_CHANGE).CopyTo(buffer, 2);

        buffer[4] = (byte)targetTeam;
        buffer[5] = targetSlotIndex;

        client.Send(buffer);
    }

    public void SendRoomSlotSwapResponse(uint requesterPlayerId, bool accept)
    {
        Debug.Log($"[CLIENT] SendRoomSlotSwapResponse requester={requesterPlayerId}, accept={accept}");

        byte[] buffer = new byte[4 + 4 + 1];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_SLOT_SWAP_RESPONSE).CopyTo(buffer, 2);

        BitConverter.GetBytes(requesterPlayerId).CopyTo(buffer, 4);
        buffer[8] = accept ? (byte)1 : (byte)0;

        client.Send(buffer);
    }

    void HandleRoomSlotChangeResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        RoomSlotTeam targetTeam = (RoomSlotTeam)body[offset];
        offset += 1;

        byte targetSlotIndex = body[offset];
        offset += 1;

        string targetNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log(
            $"[ROOM SLOT CHANGE RESULT] " +
            $"result={result}, targetTeam={targetTeam}, targetSlot={targetSlotIndex}, targetNickname={targetNickname}"
        );

        gamePanelController?.OnRecvRoomSlotChangeResult(
            result,
            targetTeam,
            targetSlotIndex,
            targetNickname
        );
    }

    void HandleRoomSlotSwapRequest(byte[] body)
    {
        int offset = 0;

        RoomSlotSwapRequestData data = new RoomSlotSwapRequestData();

        data.requesterPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        data.requesterNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        data.requesterTeam = (RoomSlotTeam)body[offset];
        offset += 1;

        data.requesterSlotIndex = body[offset];
        offset += 1;

        data.targetTeam = (RoomSlotTeam)body[offset];
        offset += 1;

        data.targetSlotIndex = body[offset];
        offset += 1;

        Debug.Log(
            $"[ROOM SLOT SWAP REQUEST] " +
            $"requester={data.requesterNickname}, requesterId={data.requesterPlayerId}, " +
            $"from={data.requesterTeam}{data.requesterSlotIndex}, " +
            $"target={data.targetTeam}{data.targetSlotIndex}"
        );

        gamePanelController?.OnRecvRoomSlotSwapRequest(data);
    }

    public void SendRoomTransferLeader(uint targetPlayerId)
    {
        Debug.Log($"[CLIENT] SendRoomTransferLeader target={targetPlayerId}");

        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_TRANSFER_LEADER).CopyTo(buffer, 2);
        BitConverter.GetBytes(targetPlayerId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    public void SendRoomKick(uint targetPlayerId)
    {
        Debug.Log($"[CLIENT] SendRoomKick target={targetPlayerId}");

        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_KICK).CopyTo(buffer, 2);
        BitConverter.GetBytes(targetPlayerId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    void HandleRoomTransferLeaderResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        uint targetPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        string targetNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log(
            $"[ROOM TRANSFER LEADER RESULT] " +
            $"result={result}, targetPlayerId={targetPlayerId}, targetNickname={targetNickname}"
        );

        gamePanelController?.OnRecvRoomTransferLeaderResult(
            result,
            targetPlayerId,
            targetNickname
        );
    }

    void HandleRoomKickResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        uint targetPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        string targetNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log(
            $"[ROOM KICK RESULT] " +
            $"result={result}, targetPlayerId={targetPlayerId}, targetNickname={targetNickname}"
        );

        gamePanelController?.OnRecvRoomKickResult(
            result,
            targetPlayerId,
            targetNickname
        );
    }

    void HandleRoomKicked(byte[] body)
    {
        int offset = 0;

        uint roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        string kickerNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log($"[ROOM KICKED] roomId={roomId}, kicker={kickerNickname}");

        ClearPrivateRoomChatContext(roomId);

        gamePanelController?.OnRecvRoomKicked(roomId, kickerNickname);
    }

    public void SendRoomMapSet(GameMapId mapId)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_MAP_SET).CopyTo(buffer, 2);
        buffer[4] = (byte)mapId;
        client.Send(buffer);
        Debug.Log($"[CLIENT] SendRoomMapSet map={mapId}");
    }

    private void HandleRoomMapSetResult(byte[] body)
    {
        if (body == null || body.Length < 2)
        {
            Debug.LogWarning("[ROOM MAP SET] Invalid response.");
            return;
        }

        byte result = body[0];
        GameMapId mapId = (GameMapId)body[1];
        gamePanelController?.OnRecvRoomMapSetResult(result, mapId);
    }
    public void SendRoomGameStart()
    {
        Debug.Log("[CLIENT] SendRoomGameStart");

        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_GAME_START).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    void HandleRoomGameStartResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        Debug.Log($"[ROOM GAME START RESULT] result={result}");

        gamePanelController?.OnRecvRoomGameStartResult(result);
    }

    public void SetPickManager(PickManager manager)
    {
        pickManager = manager;
    }

    public void SendPickEnter()
    {
        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PICK_ENTER).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    public void SendPickHover(int characterId)
    {
        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PICK_HOVER).CopyTo(buffer, 2);
        BitConverter.GetBytes(characterId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    public void SendPickLock(int characterId)
    {
        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PICK_LOCK).CopyTo(buffer, 2);
        BitConverter.GetBytes(characterId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    private void HandlePickState(byte[] body)
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.EnterPickPhase();
        }

        int offset = 0;

        PickStateData state = new PickStateData();

        state.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        state.mapName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        state.myTeam = (RoomSlotTeam)body[offset];
        offset += 1;

        state.firstRole = (PickRole)body[offset];
        offset += 1;

        state.secondRole = (PickRole)body[offset];
        offset += 1;

        state.remainingSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        state.blueScore = body[offset];
        offset += 1;

        state.redScore = body[offset];
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < memberCount; i++)
        {
            PickTeamMemberData member = new PickTeamMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.slotIndex = body[offset];
            offset += 1;

            member.hoverCharacterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.lockedCharacterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.isLocked = body[offset] != 0;
            offset += 1;

            member.isMe = body[offset] != 0;
            offset += 1;

            if (i < memberCount)
            {
                state.members.Add(member);

                if (member.isMe)
                {
                    myPlayerId = member.playerId;
                }
            }
        }

        pickManager?.OnRecvPickState(state);
    }

    private void HandlePickLockResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        int characterId = BitConverter.ToInt32(body, offset);
        offset += 4;

        pickManager?.OnRecvPickLockResult(result, characterId);
    }

    private void HandlePickTimer(byte[] body)
    {
        int offset = 0;

        PickPhase phase = (PickPhase)body[offset];
        offset += 1;

        ushort remainingSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log($"[PICK TIMER] phase={phase}, remain={remainingSeconds}");

        pickManager?.OnRecvPickTimer(phase, remainingSeconds);
    }

    private void HandleMatchLoadingInfo(byte[] body)
    {
        resultMapName = null; // New match must never reuse the previous result's map.
        if (body != null && body.Length >= 4) BeginResultAppearance(BitConverter.ToUInt32(body, 0));
        if (GameController.Instance != null)
        {
            GameController.Instance.EnterLoadingPhase();
        }

        int offset = 0;

        MatchLoadingInfoData data = new MatchLoadingInfoData();

        data.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        data.attackTeam = body[offset];
        offset += 1;

        data.defenseTeam = body[offset];
        offset += 1;

        byte attackCount = body[offset];
        offset += 1;

        byte defenseCount = body[offset];
        offset += 1;

        for (int i = 0; i < 3; i++)
        {
            MatchLoadingMemberData member = new MatchLoadingMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (i < attackCount)
                data.attackMembers.Add(member);
        }

        for (int i = 0; i < 3; i++)
        {
            MatchLoadingMemberData member = new MatchLoadingMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (i < defenseCount)
                data.defenseMembers.Add(member);
        }

        pickManager?.OnRecvMatchLoadingInfo(data);
    }

    private void HandleMatchStart(byte[] body)
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.EnterLoadingPhase();
        }

        uint roomId =
            BitConverter.ToUInt32(body, 0);

        Debug.Log($"[MATCH START] roomId={roomId}");

        pickManager?.OnRecvMatchStart(roomId);
    }

    public void SendGameLoadReady()
    {
        if (ReconnectBusy) return; // Reconnect has its own two-stage ready handshake.
        byte[] buffer = new byte[4];

        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_GAME_LOAD_READY;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);

        Debug.Log("[SEND] GAME LOAD READY");
    }

    private void HandleGameAllReady(byte[] body)
    {
        movementRound=1;
        Debug.Log("[RECV] GAME ALL READY");

        SkillInventoryState.Reset();
        playerCombatSnapshots.Clear();

        int offset = 0;

        GameAllReadyData data = new GameAllReadyData();

        data.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        data.mapName = Packet.ReadFixedString(body, offset, 32);
        resultMapName = data.mapName;
        offset += 32;

        data.prepareSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < 6; i++)
        {
            GameReadyMemberData member =
                new GameReadyMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.userId = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.team = body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;

            for (int weaponId = 0; weaponId < WeaponSkinWeaponCount; weaponId++)
            {
                member.weaponSkinLoadout[weaponId] = BitConverter.ToInt32(body, offset);
                offset += 4;
            }

            Debug.Log(
                $"[GAME ALL READY MEMBER PARSED] " +
                $"index={i}, " +
                $"playerId={member.playerId}, " +
                $"userId={member.userId}, " +
                $"nickname={member.nickname}, " +
                $"team={member.team}, " +
                $"slot={member.slotIndex}, " +
                $"characterId={member.characterId}, " +
                $"rankImageId={member.rankImageId}"
            );

            if (i < memberCount)
            {
                data.members.Add(member);
            }
        }

        Debug.Log(
            $"[RECV] GAME ALL READY " +
            $"roomId={data.roomId}, " +
            $"memberCount={data.members.Count}"
        );

        if (matchManager == null)
        {
            Debug.LogError(
                "[NETWORK] matchManager is null"
            );

            return;
        }

        matchManager.OnRecvGameAllReady(
            data
        );
        reconnectRoster = data;

        foreach (GameReadyMemberData member in data.members)
        {
            ApplyTeamToPlayer(member.playerId, member.team);

            if (member.playerId == MyPlayerId && MiniMapCamera.Instance != null)
            {
                byte firstRoundSpawnTeam = member.team == 0 ? (byte)0 : (byte)1;

                MiniMapCamera.Instance.SetLocalRoundRole(firstRoundSpawnTeam);
            }
        }

        RefreshPlayerTagsByTeam();

        if (GameController.Instance != null)
        {
            GameController.Instance.EnterPlayingPhase();
        }
        else
        {
            Debug.LogWarning(
                "[GAME ALL READY] " +
                "GameController.Instance is null"
            );
        }

        Debug.Log(
            "[GAME ALL READY] " +
            "all player data applied, " +
            "game state changed to Playing"
        );
    }

    private void HandleGamePrepareTimer(byte[] body)
    {
        int offset = 0;

        ushort remainingSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log($"[GAME PREPARE TIMER] remaining={remainingSeconds}");

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.SetTimer(remainingSeconds);

            if (remainingSeconds == 0)
            {
                TopInfoUI.Instance.SetGameDescription("전투 단계");
            }
        }

        if (matchManager != null)
        {
            matchManager.OnRecvGamePrepareTimer(remainingSeconds);
        }
        else
        {
            Debug.LogError("[NETWORK] matchManager is null. PrepareTimer ignored.");
        }
    }

    public void SendMoveState(
    float x,
    float y,
    float z,
    float yaw,
    float pitch,
    byte animIndex,
    bool isCrouching)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 1 + 1 + 8;
        ushort type = (ushort)PacketType.PKT_CS_MOVE_STATE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(yaw), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(pitch), 0, buffer, offset, 4);
        offset += 4;

        buffer[offset] = animIndex;
        offset += 1;

        buffer[offset] = isCrouching ? (byte)1 : (byte)0;
        offset += 1;

        Buffer.BlockCopy(BitConverter.GetBytes(Time.realtimeSinceStartupAsDouble),0,buffer,offset,8);

        client.Send(buffer);
    }

    private void HandleMoveState(byte[] body)
    {
        if (body==null || (body.Length!=26 && body.Length!=34)) return;
        int offset = 0;

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float yaw = BitConverter.ToSingle(body, offset);
        offset += 4;

        float pitch = BitConverter.ToSingle(body, offset);
        offset += 4;

        byte animIndex = body[offset];
        offset += 1;

        bool isCrouching = body[offset] != 0; offset += 1;
        if (playerId == MyPlayerId)
            return;

        PlayerMovement movement = FindPlayerMovementById(playerId);

        if (movement == null)
        {
            Debug.LogWarning($"[MOVE RECV] PlayerMovement not found. playerId={playerId}");
            return;
        }

        movement.OnReceiveRemoteMove(
            new Vector3(x, y, z),
            yaw,
            pitch,
            animIndex,
            isCrouching,
            body.Length==34 ? BitConverter.ToDouble(body,26) : -1
        );
    }

    private void HandleKnifeAttackNotify(
    byte[] body)
    {
        if (body == null ||
            body.Length < 4)
        {
            Debug.LogWarning(
                "[KNIFE ATTACK NOTIFY] invalid body"
            );

            return;
        }

        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(
                body,
                offset
            );
        offset += 4;


        if (playerId == MyPlayerId)
            return;

        PlayerIdentity identity =
            FindPlayerIdentityById(
                playerId
            );

        if (identity == null)
        {
            Debug.LogWarning(
                $"[KNIFE ATTACK NOTIFY] " +
                $"PlayerIdentity not found. " +
                $"playerId={playerId}"
            );

            return;
        }


        PlayerMovement movement =
            identity.GetComponent<PlayerMovement>();

        if (movement == null)
        {
            movement =
                identity.GetComponentInChildren<PlayerMovement>(
                    true
                );
        }

        if (movement != null)
        {
            movement.PlayKnifeAttackAnimation();
        }


        PlayerItem playerItem =
            identity.GetComponent<PlayerItem>();

        if (playerItem == null)
        {
            playerItem =
                identity.GetComponentInChildren<PlayerItem>(
                    true
                );
        }

        if (playerItem != null)
        {
            playerItem.PlayKnifeAttackVisualFromServer();
        }
        else
        {
            Debug.LogWarning(
                $"[KNIFE ATTACK NOTIFY] " +
                $"PlayerItem not found. " +
                $"playerId={playerId}"
            );
        }

        Debug.Log(
            $"[KNIFE ATTACK NOTIFY] " +
            $"playerId={playerId}, " +
            $"thirdPerson={(movement != null)}, " +
            $"firstPerson={(playerItem != null)}"
        );
    }

    private void HandleHPUpdate(byte[] body)
    {
        if (body == null || body.Length < 20)
        {
            Debug.LogWarning($"[HP UPDATE] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int hp = BitConverter.ToInt32(body, offset);
        offset += 4;

        int barrier = BitConverter.ToInt32(body, offset);
        offset += 4;

        int maxHp = BitConverter.ToInt32(body, offset);
        offset += 4;

        int maxBarrier = BitConverter.ToInt32(body, offset);
        offset += 4;

        PlayerCombatSnapshot snapshot = GetOrCreateCombatSnapshot(playerId);

        int previousTotalHealth = snapshot.hp + snapshot.barrier;
        int newTotalHealth = hp + barrier;

        bool tookDamage =
            snapshot.hasHpState &&
            newTotalHealth < previousTotalHealth;

        snapshot.hp = hp;
        snapshot.barrier = barrier;
        snapshot.maxHp = maxHp;
        snapshot.maxBarrier = maxBarrier;
        snapshot.hasHpState = true;

        PlayerIdentity target = FindPlayerIdentityById(playerId);

        if (target != null)
        {
            PlayerHP hpComponent = target.GetComponent<PlayerHP>();

            if (hpComponent == null)
                hpComponent = target.GetComponentInChildren<PlayerHP>(true);

            if (hpComponent != null)
            {
                hpComponent.SetServerHP(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );
            }
        }

        bool isSpectating =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating;

        uint displayedPlayerId = isSpectating
            ? SpectatorManager.Instance.CurrentTargetPlayerId
            : MyPlayerId;

        bool hpUpdateBelongsToDisplayedPlayer =
            playerId == displayedPlayerId;

        if (playerId == MyPlayerId && !isSpectating)
        {
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.SetHpBarrier(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );
            }

            if (StorePanelUI.Instance != null)
            {
                StorePanelUI.Instance.SetOwnedArmorFromServer(
                    maxBarrier
                );
            }
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                playerId
            );
        }

        if (tookDamage &&
            hpUpdateBelongsToDisplayedPlayer &&
            InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.PlayDamageFeedback();
        }

        Debug.Log(
            $"[HP UPDATE] playerId={playerId}, " +
            $"previousTotal={previousTotalHealth}, " +
            $"newTotal={newTotalHealth}, " +
            $"tookDamage={tookDamage}, " +
            $"displayedPlayerId={displayedPlayerId}"
        );
    }

    private void ApplyHPUpdate(
    uint playerId,
    int hp,
    int barrier,
    int maxHp,
    int maxBarrier)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        for (int i = 0; i < identities.Length; i++)
        {
            PlayerIdentity identity = identities[i];

            if (identity == null)
                continue;

            if (identity.PlayerId != playerId)
                continue;

            PlayerHP playerHP = identity.GetComponent<PlayerHP>();

            if (playerHP != null)
            {
                playerHP.SetServerHP(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );
            }

            break;
        }

        if (playerId == MyPlayerId)
        {
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.SetHpBarrier(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );
            }
        }
    }

    private PlayerMovement FindPlayerMovementById(uint playerId)
    {
        PlayerIdentity[] identities = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        for (int i = 0; i < identities.Length; i++)
        {
            PlayerIdentity identity = identities[i];

            if (identity == null)
                continue;

            if (identity.PlayerId != playerId)
                continue;

            PlayerMovement movement = identity.GetComponent<PlayerMovement>();

            if (movement == null)
                movement = identity.GetComponentInChildren<PlayerMovement>(true);

            return movement;
        }

        return null;
    }

    private uint nextTrackedShot = unchecked((uint)Environment.TickCount);
    private readonly Dictionary<ulong, (uint target, float time)> pendingTrackedHits = new Dictionary<ulong, (uint, float)>();

    private void HandleShotConfirmed(byte[] body)
    {
        if (body == null || body.Length != 18) return;
        uint shot = BitConverter.ToUInt32(body, 0);
        uint target = BitConverter.ToUInt32(body, 4);
        int damage = BitConverter.ToInt32(body, 12);
        byte pellet = body[16];
        ulong key = ((ulong)shot << 8) | pellet;
        if (!pendingTrackedHits.TryGetValue(key, out var pending) || pending.target != target) return;
        pendingTrackedHits.Remove(key);
        if (damage > 0 && Time.unscaledTime - pending.time <= 5f)
            HitManager.TriggerEnemyHit();
    }

    public uint SendTrackedFire(int weaponId, Vector3? origin = null)
    {
        if (StorePanelUI.IsBlockingGameplayInput) return 0;
        uint shot = unchecked(++nextTrackedShot);
        if (shot == 0) shot = unchecked(++nextTrackedShot);
        byte[] packet = new byte[origin.HasValue ? 24 : 12];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)packet.Length), 0, packet, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_FIRE), 0, packet, 2, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(shot), 0, packet, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(weaponId), 0, packet, 8, 4);
        if (origin.HasValue)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(origin.Value.x), 0, packet, 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(origin.Value.y), 0, packet, 16, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(origin.Value.z), 0, packet, 20, 4);
        }
        client.Send(packet);
        return shot;
    }

    public void SendTrackedHit(uint targetPlayerId, bool isHeadshot, uint shot, byte pellet)
        => SendTrackedHit(targetPlayerId, isHeadshot ? (byte)1 : (byte)0, shot, pellet);

    // Wire regions stay 0=body, 1=head, 2=legs; authored HitPart values differ.
    public void SendTrackedHit(uint targetPlayerId, byte hitZone, uint shot, byte pellet, Vector3? point = null)
    {
        if (shot == 0 || hitZone > 2) return;
        byte[] packet = new byte[point.HasValue ? 26 : 14];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)packet.Length), 0, packet, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_HIT), 0, packet, 2, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(targetPlayerId), 0, packet, 4, 4);
        packet[8] = hitZone;
        Buffer.BlockCopy(BitConverter.GetBytes(shot), 0, packet, 9, 4);
        packet[13] = pellet;
        if (point.HasValue)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(point.Value.x), 0, packet, 14, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(point.Value.y), 0, packet, 18, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(point.Value.z), 0, packet, 22, 4);
        }
        // Bounded even if a server rejects all requests; no speculative player hit marker.
        if (pendingTrackedHits.Count >= 2048) pendingTrackedHits.Clear();
        pendingTrackedHits[((ulong)shot << 8) | pellet] = (targetPlayerId, Time.unscaledTime);
        client.Send(packet);
    }

    public void SendHit(uint targetPlayerId, bool isHeadshot)
        => SendHit(targetPlayerId, isHeadshot ? 1 : 0);

    public void SendHit(uint targetPlayerId, int hitPart = 0)
    {
        if (hitPart < 0 || hitPart > 2) return;
        ushort size = 4 + 4 + 1;
        ushort type = (ushort)PacketType.PKT_CS_HIT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(targetPlayerId), 0, buffer, offset, 4);
        offset += 4;

        buffer[offset] = (byte)hitPart;

        client.Send(buffer);

        Debug.Log($"[SEND HIT] targetPlayerId={targetPlayerId} hitPart={hitPart}");
    }

    public void SendWeaponSwitch(int weaponId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_WEAPON_SWITCH;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(weaponId), 0, buffer, offset, 4);

        client.Send(buffer);

        Debug.Log($"[SEND WEAPON SWITCH] weaponId={weaponId}");
    }

    private void HandleRoundResult(byte[] body)
    {
        ScannerNetworkManager.Instance?.ClearAll();
        DischargeDirector.Instance?.ClearAll();
        ScannerFeedbackUI.Instance?.ResetFeedback();
        int offset = 0;

        byte winnerTeam = body[offset];
        offset += 1;

        byte scoreA = body[offset];
        offset += 1;

        byte scoreB = body[offset];
        offset += 1;

        byte currentRound = body[offset];
        offset += 1;

        byte matchEnd = body[offset];
        offset += 1;

        RoundWinReason winReason = (RoundWinReason)body[offset];
        offset += 1;

        ushort delaySeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log(
            $"[ROUND RESULT] winnerTeam={winnerTeam}, " +
            $"scoreA={scoreA}, scoreB={scoreB}, round={currentRound}, " +
            $"matchEnd={matchEnd}, delay={delaySeconds}"
        );

        byte localTeam = GetLocalTeamForRoundResult();

        if (CombatFeedbackUI.Instance != null)
        {
            CombatFeedbackUI.Instance.ShowRoundResult(
                localTeam,
                winnerTeam,
                winReason,
                scoreA,
                scoreB
            );
        }

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.StopLocalCountdown();
            TopInfoUI.Instance.SetScoreByTeam(scoreA, scoreB);
        }
        else
        {
            Debug.LogError("[ROUND RESULT] TopInfoUI.Instance is null");
        }

        if (InGameScoreboardUI.Instance != null)
        {
            InGameScoreboardUI.Instance.OnRoundResult(
                currentRound,
                winnerTeam,
                winReason,
                scoreA,
                scoreB
            );
        }

        if (matchManager != null)
        {
            matchManager.OnRecvRoundResult(
                winnerTeam,
                scoreA,
                scoreB,
                currentRound,
                matchEnd != 0,
                delaySeconds
            );
        }
        else
        {
            Debug.LogError("[NETWORK] matchManager is null. RoundResult ignored.");
        }
    }

    private void HandleRoundReset(byte[] body)
    {
        ScannerNetworkManager.Instance?.ClearAll();
        DischargeDirector.Instance?.ClearAll();
        ScannerFeedbackUI.Instance?.ResetFeedback();
        if(body!=null && body.Length>=4) movementRound=body[0];
        if (CombatFeedbackUI.Instance != null)
            CombatFeedbackUI.Instance.ResetAll();

        if (KillLogManager.Instance != null)
        {
            KillLogManager.Instance.ClearAll();
        }

        if (WeaponDropSpawner.Instance != null)
        {
            WeaponDropSpawner.Instance.ClearAllDroppedWeapons();
        }

        droppedItemMap.Clear();

        if (StorePanelUI.Instance != null)
        {
            StorePanelUI.Instance.ResetOwnedItemsForNewRound();
        }

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.ResetAllPlayerDeadVisuals();
        }

        int offset = 0;

        byte currentRound = body[offset];
        offset += 1;

        byte scoreA = body[offset];
        offset += 1;

        byte scoreB = body[offset];
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        Debug.Log(
            $"[ROUND RESET] round={currentRound}, scoreA={scoreA}, scoreB={scoreB}, memberCount={memberCount}"
        );

        ClearAllSpikeObjectsForNewRound();

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.SetScoreByTeam(scoreA, scoreB);
            TopInfoUI.Instance.SetGameDescription("구매 단계");
        }

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnServerRoundReset(
                currentRound,
                scoreA,
                scoreB
            );
        }

        for (int i = 0; i < memberCount; i++)
        {
            uint playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            byte actualTeam = body[offset];
            offset += 1;

            byte spawnTeam = body[offset];
            offset += 1;

            byte slotIndex = body[offset];
            offset += 1;

            int hp = BitConverter.ToInt32(body, offset);
            offset += 4;

            int barrier = BitConverter.ToInt32(body, offset);
            offset += 4;

            int maxHp = BitConverter.ToInt32(body, offset);
            offset += 4;

            int maxBarrier = BitConverter.ToInt32(body, offset);
            offset += 4;

            int currentWeaponId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int magAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int reserveAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int primaryWeaponId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int primaryMagAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int primaryReserveAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int primarySkinId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int pistolWeaponId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int pistolMagAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int pistolReserveAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int pistolSkinId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int knifeWeaponId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int knifeMagAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int knifeReserveAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int knifeSkinId = BitConverter.ToInt32(body, offset);
            offset += 4;

            byte purchasedSkillMask = body[offset];
            offset += 1;

            byte usedSkillMask = body[offset];
            offset += 1;

            if (playerId == MyPlayerId)
            {
                SkillInventoryState.ApplyServerState(
                    purchasedSkillMask,
                    usedSkillMask
                );
            }

            if (playerId == MyPlayerId && MiniMapCamera.Instance != null)
            {
                MiniMapCamera.Instance.SetLocalRoundRole(spawnTeam);
            }

            PlayerCombatSnapshot snapshot = GetOrCreateCombatSnapshot(playerId);

            snapshot.hp = hp;
            snapshot.barrier = barrier;
            snapshot.maxHp = maxHp;
            snapshot.maxBarrier = maxBarrier;

            snapshot.weaponId = currentWeaponId;
            snapshot.magAmmo = magAmmo;
            snapshot.reserveAmmo = reserveAmmo;
            snapshot.isADS = false;

            ApplyRoundResetMember(
                playerId,
                actualTeam,
                spawnTeam,
                slotIndex,
                hp,
                barrier,
                maxHp,
                maxBarrier,
                currentWeaponId,
                magAmmo,
                reserveAmmo
            );

            ApplyRoundResetInventoryToPlayer(
                playerId,
                currentWeaponId,
                primaryWeaponId,
                primaryMagAmmo,
                primaryReserveAmmo,
                primarySkinId,
                pistolWeaponId,
                pistolMagAmmo,
                pistolReserveAmmo,
                pistolSkinId,
                knifeWeaponId,
                knifeMagAmmo,
                knifeReserveAmmo,
                knifeSkinId
            );
        }

        RefreshPlayerTagsByTeam();
    }

    private void ApplyRoundResetMember(
    uint playerId,
    byte actualTeam,
    byte spawnTeam,
    byte slotIndex,
    int hp,
    int barrier,
    int maxHp,
    int maxBarrier,
    int currentWeaponId,
    int magAmmo,
    int reserveAmmo)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        PlayerIdentity targetIdentity = null;

        for (int i = 0; i < identities.Length; i++)
        {
            if (identities[i] == null)
                continue;

            if (identities[i].PlayerId == playerId)
            {
                targetIdentity = identities[i];
                break;
            }
        }

        if (targetIdentity == null)
        {
            Debug.LogWarning($"[ROUND RESET] player not found. playerId={playerId}");
            return;
        }

        targetIdentity.Team = actualTeam;

        bool isMine = playerId == MyPlayerId;

        if (isMine && SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.StopSpectating();
        }

        MapInfo mapInfo = FindFirstObjectByType<MapInfo>();

        if (mapInfo == null)
        {
            Debug.LogError("[ROUND RESET] MapInfo not found");
            return;
        }

        Transform spawnPoint = mapInfo.GetSpawnPoint(spawnTeam, slotIndex);

        if (spawnPoint == null)
        {
            Debug.LogError($"[ROUND RESET] spawnPoint not found slot={slotIndex}");
            return;
        }

        CharacterController cc =
            targetIdentity.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        targetIdentity.transform.position = spawnPoint.position;
        targetIdentity.transform.rotation = spawnPoint.rotation;

        if (cc != null)
            cc.enabled = true;

        PlayerHP playerHP = targetIdentity.GetComponent<PlayerHP>();

        if (playerHP != null)
        {
            playerHP.SetServerHP(
                hp,
                barrier,
                maxHp,
                maxBarrier
            );
        }

        PlayerDeathController death =
            targetIdentity.GetComponent<PlayerDeathController>();

        if (death != null)
            death.OnRespawn();

        PlayerMovement movement =
            targetIdentity.GetComponent<PlayerMovement>();

        PlayerCameraController cameraController =
            targetIdentity.GetComponent<PlayerCameraController>();

        if (isMine)
        {
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.SetHpBarrier(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );

                InGameUIManager.Instance.SetBulletText(
                    magAmmo,
                    reserveAmmo
                );
            }

            if (movement != null)
            {
                movement.SetLocalPlayer(true);
                movement.CanMove = true;
            }

            if (cameraController != null)
            {
                cameraController.SetLocalPlayer(true);
            }

            FPSCameraLook look =
                targetIdentity.GetComponentInChildren<FPSCameraLook>(true);

            if (look != null)
                look.ResetLookRotation(spawnPoint.rotation.eulerAngles.y);
        }
        else
        {
            if (movement != null)
                movement.SetLocalPlayer(false);

            if (cameraController != null)
                cameraController.SetLocalPlayer(false);
        }

        Debug.Log(
            $"[ROUND RESET APPLY] playerId={playerId}, team={actualTeam}, slot={slotIndex}, pos={spawnPoint.position}, isMine={isMine}"
        );
    }

    private void ApplyRoundResetInventoryToPlayer(
    uint playerId,
    int currentWeaponId,
    int primaryWeaponId,
    int primaryMagAmmo,
    int primaryReserveAmmo,
    int primarySkinId,
    int pistolWeaponId,
    int pistolMagAmmo,
    int pistolReserveAmmo,
    int pistolSkinId,
    int knifeWeaponId,
    int knifeMagAmmo,
    int knifeReserveAmmo,
    int knifeSkinId)
    {
        PlayerIdentity target = FindPlayerIdentityById(playerId);

        if (target == null)
        {
            Debug.LogWarning($"[ROUND RESET INVENTORY] player not found. playerId={playerId}");
            return;
        }

        PlayerItem item = target.GetComponent<PlayerItem>();

        if (item == null)
            item = target.GetComponentInChildren<PlayerItem>(true);

        if (item == null)
        {
            Debug.LogWarning($"[ROUND RESET INVENTORY] PlayerItem not found. playerId={playerId}");
            return;
        }

        item.ApplyRoundResetInventoryFromServer(
            currentWeaponId,
            primaryWeaponId,
            primaryMagAmmo,
            primaryReserveAmmo,
            primarySkinId,
            pistolWeaponId,
            pistolMagAmmo,
            pistolReserveAmmo,
            pistolSkinId,
            knifeWeaponId,
            knifeMagAmmo,
            knifeReserveAmmo,
            knifeSkinId
        );

        // Reapply the current weapon only after the round inventory is rebuilt.
        item.SetWeaponVisibility(true);

        PlayerCameraController roundResetCamera =
            target.GetComponent<PlayerCameraController>();

        if (roundResetCamera == null)
        {
            roundResetCamera =
                target.GetComponentInChildren<PlayerCameraController>(true);
        }

        if (roundResetCamera != null)
            roundResetCamera.RefreshViewVisuals();

        if (playerId == MyPlayerId)
        {
            if (StorePanelUI.Instance != null)
            {
                StorePanelUI.Instance.ResetOwnedItemsForNewRound();

                if (primaryWeaponId >= 0)
                    StorePanelUI.Instance.SetOwnedWeaponFromServer(primaryWeaponId);

                if (pistolWeaponId >= 0)
                    StorePanelUI.Instance.SetOwnedWeaponFromServer(pistolWeaponId);

                if (knifeWeaponId >= 0)
                    StorePanelUI.Instance.SetOwnedWeaponFromServer(knifeWeaponId);

                // 방어구를 0으로 초기화
                StorePanelUI.Instance.SetOwnedArmorFromServer(0);
            }
        }

        Debug.Log(
            $"[ROUND RESET INVENTORY] playerId={playerId}, current={currentWeaponId}, " +
            $"primary={primaryWeaponId}, pistol={pistolWeaponId}, knife={knifeWeaponId}"
        );
    }

    private void HandleMatchResult(byte[] body)
    {
        int offset = 0;

        byte winnerTeam = body[offset];
        offset += 1;

        byte scoreA = body[offset];
        offset += 1;

        byte scoreB = body[offset];
        offset += 1;

        Debug.Log(
            $"[MATCH RESULT] winnerTeam={winnerTeam}, scoreA={scoreA}, scoreB={scoreB}"
        );

        SceneManager.LoadScene("Result");
    }

    public void SendReload()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_RELOAD;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        client.Send(buffer);

        Debug.Log("[SEND RELOAD]");
    }

    private void HandleAmmoUpdate(byte[] body)
    {
        if (body == null || body.Length < 16)
        {
            Debug.LogWarning(
                $"[AMMO UPDATE] invalid body length={body?.Length ?? 0}"
            );
            return;
        }

        int offset = 0;

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        int magAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int reserveAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        PlayerCombatSnapshot snapshot =
            GetOrCreateCombatSnapshot(playerId);

        snapshot.weaponId = weaponId;
        snapshot.magAmmo = magAmmo;
        snapshot.reserveAmmo = reserveAmmo;

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity != null)
        {
            PlayerItem item =
                identity.GetComponent<PlayerItem>();

            if (item == null)
                item = identity.GetComponentInChildren<PlayerItem>(true);

            if (item != null)
            {
                item.SetWeaponAmmoFromServer(
                    weaponId,
                    magAmmo,
                    reserveAmmo
                );
            }
        }

        bool isSpectating =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating;

        if (playerId == MyPlayerId &&
            !isSpectating &&
            InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.SetBulletText(
                magAmmo,
                reserveAmmo
            );
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                playerId
            );
        }

        Debug.Log(
            $"[AMMO UPDATE] playerId={playerId}, " +
            $"weaponId={weaponId}, ammo={magAmmo}/{reserveAmmo}"
        );
    }

    public void SendFire()
    {
        if (StorePanelUI.IsBlockingGameplayInput)
            return;

        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_FIRE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        client.Send(buffer);

        Debug.Log("[SEND FIRE]");
    }

    public void SendKnifeAttack()
    {
        if (StorePanelUI.IsBlockingGameplayInput)
            return;

        ushort size = 4;
        ushort type =
            (ushort)PacketType.PKT_CS_KNIFE_ATTACK;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );

        client.Send(buffer);

        Debug.Log("[SEND KNIFE ATTACK]");
    }

    private string resultMapName;
    private void HandleMatchResultDetail(byte[] body)
    {
        const int HeaderSize = 5;
        const int MemberEntrySize = 118;

        if (body == null || body.Length < HeaderSize)
        {
            Debug.LogWarning("[MATCH RESULT DETAIL] invalid packet header");
            return;
        }

        int expectedSize = HeaderSize + body[4] * MemberEntrySize;
        if (body.Length < expectedSize)
        {
            Debug.LogWarning(
                $"[MATCH RESULT DETAIL] invalid packet size: " +
                $"expected={expectedSize}, actual={body.Length}");
            return;
        }

        int offset = 0;

        var appearances = ConsumeResultAppearance();
        MatchResultCache.Clear();
        MatchResultCache.mapName = resultMapName;

        MatchResultCache.winnerTeam = body[offset];
        offset += 1;

        MatchResultCache.scoreA = body[offset];
        offset += 1;

        MatchResultCache.scoreB = body[offset];
        offset += 1;

        MatchResultCache.matchType = body[offset];
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < memberCount; i++)
        {
            MatchResultMemberData member = new MatchResultMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.team = body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.kills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.deaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.assists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.firstHalfKills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.firstHalfDeaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.firstHalfAssists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.secondHalfKills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.secondHalfDeaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.secondHalfAssists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.overtimeKills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.overtimeDeaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.overtimeAssists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.isMvp = body[offset] != 0;
            offset += 1;

            member.freeCurrencyReward = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.victoryCurrencyReward = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankPointDelta = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankPointsBefore = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankPointsAfter = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankTierIdBefore = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankTierIdAfter = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankExpectedWinRatePermille =
                BitConverter.ToUInt16(body, offset);
            offset += 2;
            member.rankModifierFlags = body[offset];
            offset += 1;
            member.totalFreeCurrency = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.totalVictoryCurrency = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (appearances.TryGetValue(member.playerId, out var appearance)) member.appearance = appearance;
            MatchResultCache.members.Add(member);

            if (member.playerId == MyPlayerId)
            {
                MatchResultCache.freeCurrencyReward = member.freeCurrencyReward;
                MatchResultCache.victoryCurrencyReward = member.victoryCurrencyReward;
                MatchResultCache.rankPointDelta = member.rankPointDelta;
                MatchResultCache.rankPointsBefore = member.rankPointsBefore;
                MatchResultCache.rankPointsAfter = member.rankPointsAfter;
                MatchResultCache.rankTierIdBefore = member.rankTierIdBefore;
                MatchResultCache.rankTierIdAfter = member.rankTierIdAfter;
                MatchResultCache.rankExpectedWinRatePermille =
                    member.rankExpectedWinRatePermille;
                MatchResultCache.rankModifierFlags =
                    member.rankModifierFlags;
                MatchResultCache.totalFreeCurrency = member.totalFreeCurrency;
                MatchResultCache.totalVictoryCurrency = member.totalVictoryCurrency;
                MoneyManager.Instance?.SetServerBalances(
                    member.totalFreeCurrency,
                    member.totalVictoryCurrency
                );
            }
        }

        Debug.Log(
            $"[MATCH RESULT DETAIL] winner={MatchResultCache.winnerTeam}, " +
            $"score={MatchResultCache.scoreA}:{MatchResultCache.scoreB}, " +
            $"members={MatchResultCache.members.Count}"
        );

        // The authored Result scene now owns character -> rank -> rewards -> table.
        // Settlement/cache/wallet updates above remain authoritative and unchanged.
        SceneManager.LoadScene("Result");
    }

    public void SendResultLeave()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_RESULT_LEAVE;

        byte[] buffer = new byte[size];

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);

        Debug.Log("[SEND RESULT LEAVE]");
    }

    private void HandleResultLeaveDone(byte[] body)
    {
        uint roomId = BitConverter.ToUInt32(body, 0);

        Debug.Log($"[RESULT LEAVE DONE] roomId={roomId}");

        pendingReturnRoomId = roomId;
        ReturningToParty = roomId == 0;

        SceneManager.LoadScene("Lobby");
    }

    public void SendRoomStateRequest(uint roomId)
    {
        ushort size = 8;
        ushort type = (ushort)PacketType.PKT_CS_ROOM_STATE_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(roomId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND ROOM STATE REQUEST] roomId={roomId}");
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[NETWORK] OnApplicationQuit");
        client?.Close();
    }

    private void HandleScoreboardUpdate(byte[] body)
    {
        int offset = 0;

        ScoreboardData data = new ScoreboardData();

        data.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        data.myTeam = body[offset];
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < memberCount; i++)
        {
            ScoreboardMemberData member = new ScoreboardMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.team = body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.kills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.deaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.assists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.credits = BitConverter.ToInt32(body, offset);
            offset += 4;

            data.members.Add(member);
        }

        latestScoreboardData = data;

        if (InGameScoreboardUI.Instance != null)
            InGameScoreboardUI.Instance.OnRecvScoreboardUpdate(data);

        if (StorePanelUI.Instance != null)
            StorePanelUI.Instance.RefreshPlayerInfo();
    }

    private void HandleRoundTimer(byte[] body)
    {
        int offset = 0;

        ushort remainingSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log($"[ROUND TIMER] remaining={remainingSeconds}");

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.SetGameDescription("전투 단계");
            TopInfoUI.Instance.SetTimer(remainingSeconds);
        }
        else
        {
            Debug.LogError("[ROUND TIMER] TopInfoUI.Instance is null");
        }
    }

    public void SendSpikePickupRequest()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_SPIKE_PICKUP_REQUEST;

        byte[] buffer = new byte[size];

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);

        Debug.Log("[SEND] SPIKE PICKUP REQUEST");
    }

    public void SendSpikePlantComplete(Vector3 position)
    {
        ushort size = 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SPIKE_PLANT_COMPLETE;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);

        client.Send(buffer);

        Debug.Log($"[SEND] SPIKE PLANT COMPLETE pos={position}");
    }

    public void SendSpikePlantStart() => SendEmptyPacket(PacketType.PKT_CS_SPIKE_PLANT_START);
    public void SendSpikePlantCancel() => SendEmptyPacket(PacketType.PKT_CS_SPIKE_PLANT_CANCEL);

    private void HandleSpikePickupNotify(byte[] body)
    {
        int offset = 0;

        uint carrierPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte showCarrierInfo = body[offset];
        offset += 1;

        string carrierNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log($"[SPIKE PICKUP NOTIFY] carrier={carrierPlayerId}, show={showCarrierInfo}, nickname={carrierNickname}");

        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        foreach (PlayerIdentity identity in identities)
        {
            PlayerSpike ps = identity.GetComponent<PlayerSpike>();
            if (ps == null)
                continue;

            bool hasSpike = identity.PlayerId == carrierPlayerId;
            ps.SetHasSpikeFromServer(hasSpike);
        }

        // 핵심: 모든 클라에서 바닥 스파이크 제거
        DroppedSpikePickup[] droppedSpikes =
            FindObjectsByType<DroppedSpikePickup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (DroppedSpikePickup dropped in droppedSpikes)
        {
            Rigidbody rb = dropped.GetComponentInParent<Rigidbody>();

            if (rb != null)
                Destroy(rb.gameObject);
            else
                Destroy(dropped.transform.root.gameObject);
        }

        if (TopInfoUI.Instance != null)
        {
            if (showCarrierInfo != 0)
                TopInfoUI.Instance.SetGameDescription($"{carrierNickname} 스파이크 획득");
            else
                TopInfoUI.Instance.SetGameDescription("스파이크 획득됨");
        }
    }
    private void HandleSpikePlanted(byte[] body)
    {
        if (body == null || body.Length != 12) return;
        if (RoundManager.Instance != null && (RoundManager.Instance.currentState == RoundManager.RoundState.RoundEnd ||
            RoundManager.Instance.currentState == RoundManager.RoundState.GameOver)) return;
        int offset = 0;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 pos = new Vector3(x, y, z);

        Debug.Log($"[SPIKE PLANTED] pos={pos}");

        if (SpikeSpawner.Instance != null)
            SpikeSpawner.Instance.SpawnPlanted(pos);

        PlayerSpike mySpike = FindMyPlayerSpike();
        if (mySpike != null)
            mySpike.SetHasSpikeFromServer(false);

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.SetGameDescription("스파이크 설치됨");
            TopInfoUI.Instance.SetUnknownTimer();
        }

        if (CombatFeedbackUI.Instance != null)
        {
            CombatFeedbackUI.Instance.HideSpikeProgress();

            CombatFeedbackUI.Instance.ShowObjectiveNotice(
                "공격팀이 스파이크를 설치했습니다.",
                true
            );
        }
    }

    private PlayerSpike FindMyPlayerSpike()
    {
        PlayerIdentity[] players = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        foreach (PlayerIdentity identity in players)
        {
            if (identity.PlayerId == MyPlayerId)
                return identity.GetComponent<PlayerSpike>();
        }

        return null;
    }

    private void HandleSpikeDropped(byte[] body)
    {
        int offset = 0;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        float dirX = BitConverter.ToSingle(body, offset); offset += 4;
        float dirY = BitConverter.ToSingle(body, offset); offset += 4;
        float dirZ = BitConverter.ToSingle(body, offset); offset += 4;

        uint dropperPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        Vector3 pos = new Vector3(x, y, z);
        Vector3 dir = new Vector3(dirX, dirY, dirZ);

        if (SpikeSpawner.Instance != null)
            SpikeSpawner.Instance.SpawnDroppedAt(pos, dir, dropperPlayerId);

        // 핵심: 스파이크를 버린 플레이어는 더 이상 스파이크 소유자가 아님
        ApplySpikeDroppedToPlayer(dropperPlayerId);
    }

    private void HandleSpikeDefuseState(byte[] body)
    {
        if (body == null || body.Length != 6) return;
        int offset = 0;

        byte state = body[offset];
        offset += 1;

        byte halfDefused = body[offset];
        offset += 1;

        uint defuserPlayerId = BitConverter.ToUInt32(body, offset);

        Spike[] spikes = FindObjectsByType<Spike>(FindObjectsSortMode.None);

        for (int i = 0; i < spikes.Length; i++)
        {
            if (state == 1)
            {
                if (defuserPlayerId == MyPlayerId)
                    spikes[i].StartDefuseFromServer(halfDefused != 0);
            }
            else if (state == 0)
            {
                spikes[i].CancelDefuseFromServer(halfDefused != 0);
            }
            else if (state == 2)
            {
                spikes[i].SetHalfDefusedFromServer(true);
            }
        }
    }

    private void HandleSpikeDefused(byte[] body)
    {
        int offset = 0;

        uint defuserPlayerId = BitConverter.ToUInt32(body, offset);

        Spike[] spikes = FindObjectsByType<Spike>(FindObjectsSortMode.None);

        for (int i = 0; i < spikes.Length; i++)
        {
            Destroy(spikes[i].gameObject);
        }

        if (TopInfoUI.Instance != null)
            TopInfoUI.Instance.SetGameDescription("스파이크 해체됨");

        if (CombatFeedbackUI.Instance != null)
        {
            CombatFeedbackUI.Instance.HideSpikeProgress();

            // 기존 설치 상태 아이콘 제거
            CombatFeedbackUI.Instance.ClearObjectiveState();

            // 해체 완료 메시지만 잠깐 표시
            CombatFeedbackUI.Instance.ShowObjectiveNotice(
                "수비팀이 스파이크를 해체했습니다.",
                false
            );
        }

        Debug.Log($"[SPIKE DEFUSED] defuser={defuserPlayerId}");
    }

    public void SendSpikeDefuseStart()
    {
        SendEmptyPacket(PacketType.PKT_CS_SPIKE_DEFUSE_START);
    }

    public void SendSpikeDefuseCancel()
    {
        SendEmptyPacket(PacketType.PKT_CS_SPIKE_DEFUSE_CANCEL);
    }

    public void SendSpikeDefuseHalf()
    {
        SendEmptyPacket(PacketType.PKT_CS_SPIKE_DEFUSE_HALF);
    }

    public void SendSpikeDefuseComplete()
    {
        SendEmptyPacket(PacketType.PKT_CS_SPIKE_DEFUSE_COMPLETE);
    }

    private void SendEmptyPacket(PacketType packetType)
    {
        ushort size = 4;
        ushort type = (ushort)packetType;

        byte[] buffer = new byte[size];

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);

        Debug.Log($"[SEND] {packetType}");
    }

    private void HandleDie(byte[] body)
    {
        int offset = 0;

        uint killerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint deadPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte killerTeam = body[offset];
        offset += 1;

        byte deadTeam = body[offset];
        offset += 1;

        int killerCharacterId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        int deadCharacterId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        string killerNickname =
            Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        string deadNickname =
            Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log(
            $"[DIE] killer={killerPlayerId}, " +
            $"dead={deadPlayerId}, " +
            $"killerTeam={killerTeam}, " +
            $"deadTeam={deadTeam}, " +
            $"killerNickname={killerNickname}, " +
            $"deadNickname={deadNickname}, " +
            $"killerCharacter={killerCharacterId}, " +
            $"deadCharacter={deadCharacterId}"
        );

        // 킬러가 정상적으로 존재하는 경우만 킬로그 표시
        if (killerPlayerId != 0 &&
            KillLogManager.Instance != null)
        {
            byte myTeam = GetMyTeamForKillLog();

            // 킬러가 내 팀이면 OurKillLogPrefab
            // 킬러가 상대 팀이면 EnemyKillLogPrefab
            bool isOurKill =
                killerTeam == myTeam;

            KillLogManager.Instance.ShowKillLog(
                isOurKill,
                killerCharacterId,
                deadCharacterId,
                killerNickname,
                deadNickname
            );
        }

        PlayerIdentity deadIdentity =
            FindPlayerIdentityById(deadPlayerId);

        if (deadIdentity != null)
        {
            PlayerDeathController death =
                deadIdentity.GetComponent<PlayerDeathController>();

            if (death == null)
            {
                death =
                    deadIdentity.GetComponentInChildren<PlayerDeathController>(
                        true
                    );
            }

            if (death != null)
            {
                death.OnDead();
            }
            else
            {
                Debug.LogError(
                    $"[DIE] PlayerDeathController missing. " +
                    $"object={deadIdentity.name}"
                );
            }
        }

        if (deadPlayerId != MyPlayerId)
            return;

        PlayerIdentity myIdentity =
            FindPlayerIdentityById(MyPlayerId);

        if (myIdentity != null)
        {
            PlayerSpike playerSpike =
                myIdentity.GetComponent<PlayerSpike>();

            if (playerSpike == null)
            {
                playerSpike =
                    myIdentity.GetComponentInChildren<PlayerSpike>(true);
            }

            if (playerSpike != null)
            {
                playerSpike.ForceCancelPlant();
            }
            else
            {
                Debug.LogError(
                    "[DIE] PlayerSpike missing on local player"
                );
            }

            PlayerItem item =
                myIdentity.GetComponent<PlayerItem>();

            if (item != null)
                item.SetWeaponVisibility(false);

            FPSCameraLook cameraLook =
                myIdentity.GetComponentInChildren<FPSCameraLook>(true);

            if (cameraLook != null)
            {
                cameraLook.ForceExitADS();
            }
            else
            {
                Debug.LogError(
                    "[DIE] FPSCameraLook missing on local player"
                );
            }
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.StartSpectating();
        }
        else
        {
            Debug.LogError(
                "[DIE] SpectatorManager.Instance is null"
            );
        }
    }

    public void SendSpikeDropRequest(Vector3 position, Vector3 direction)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SPIKE_DROP_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(direction.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(direction.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(direction.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND] SPIKE DROP REQUEST pos={position}, dir={direction}");
    }

    private void ApplyTeamToPlayer(uint playerId, byte team)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity identity in identities)
        {
            if (identity == null)
                continue;

            if (identity.PlayerId != playerId)
                continue;

            identity.Team = team;

            Debug.Log(
                $"[TEAM APPLY] playerId={playerId}, team={team}, object={identity.gameObject.name}"
            );

            return;
        }

        Debug.LogWarning(
            $"[TEAM APPLY FAIL] player not found. playerId={playerId}, team={team}"
        );
    }

    private void RefreshPlayerTagsByTeam()
    {
        PlayerIdentity myIdentity = null;

        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity identity in identities)
        {
            if (identity == null)
                continue;

            if (identity.PlayerId == MyPlayerId)
            {
                myIdentity = identity;
                break;
            }
        }

        if (myIdentity == null)
            return;

        foreach (PlayerIdentity identity in identities)
        {
            if (identity == null)
                continue;

            if (identity.Team == myIdentity.Team)
                identity.gameObject.tag = "Player";
            else
                identity.gameObject.tag = "Enemy";

            Debug.Log($"[TAG REFRESH] playerId={identity.PlayerId}, team={identity.Team}, tag={identity.gameObject.tag}");
        }
    }

    private PlayerIdentity FindPlayerIdentityById(uint playerId)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity identity in identities)
        {
            if (identity == null)
                continue;

            if (identity.PlayerId == playerId)
                return identity;
        }

        return null;
    }

    private void HandleWeaponSwitchNotify(byte[] body)
    {
        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        Debug.Log(
            $"[WEAPON SWITCH NOTIFY] " +
            $"playerId={playerId}, weaponId={weaponId}"
        );

        PlayerCombatSnapshot snapshot =
            GetOrCreateCombatSnapshot(playerId);

        snapshot.weaponId = weaponId;
        snapshot.isADS = false;

        // 로컬은 무기 Switch에서 이미 처리함
        if (playerId == MyPlayerId)
        {
            Debug.Log(
                $"[WEAPON SWITCH NOTIFY] " +
                $"ignored self. weaponId={weaponId}"
            );

            return;
        }

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity != null)
        {
            PlayerItem item =
                identity.GetComponent<PlayerItem>();

            if (item == null)
            {
                item =
                    identity.GetComponentInChildren<PlayerItem>(
                        true
                    );
            }

            if (item != null)
            {
                item.ApplyWeaponSwitchFromServer(
                    weaponId
                );

                // 무기 변경 후 기존 ADS 상태 해제
                item.SetADSVisualFromServer(false);
            }
            else
            {
                Debug.LogWarning(
                    $"[WEAPON SWITCH NOTIFY] " +
                    $"PlayerItem missing. playerId={playerId}"
                );
            }

            FPSCameraLook look =
                identity.GetComponentInChildren<FPSCameraLook>(
                    true
                );

            if (look != null)
            {
                look.SetADSFromServer(false);
            }
        }
        else
        {
            Debug.LogWarning(
                $"[WEAPON SWITCH NOTIFY] " +
                $"player not found. playerId={playerId}"
            );
        }

        bool isCurrentSpectatorTarget =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating &&
            SpectatorManager.Instance.CurrentTargetPlayerId == playerId;

        if (isCurrentSpectatorTarget &&
            InGameUIManager.Instance != null)
        {
            // 무기를 변경하면 서버 스냅샷의 ADS도 false로 초기화된다.
            InGameUIManager.Instance.SetDisplayedPlayerADS(false);
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                playerId
            );
        }
    }
    public void SendADSState(bool isADS)
    {
        ushort size = 4 + 1;
        ushort type = (ushort)PacketType.PKT_CS_ADS_STATE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        buffer[offset] = isADS ? (byte)1 : (byte)0;

        client.Send(buffer);

        Debug.Log($"[SEND ADS STATE] isADS={isADS}");
    }

    private void HandleADSState(byte[] body)
    {
        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        bool isADS =
            body[offset] != 0;
        offset += 1;

        Debug.Log(
            $"[ADS STATE] " +
            $"playerId={playerId}, isADS={isADS}"
        );

        PlayerCombatSnapshot snapshot =
            GetOrCreateCombatSnapshot(playerId);

        snapshot.isADS = isADS;

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity != null)
        {
            FPSCameraLook look =
                identity.GetComponentInChildren<FPSCameraLook>(
                    true
                );

            if (look != null)
            {
                look.SetADSFromServer(
                    isADS
                );
            }

            PlayerItem item =
                identity.GetComponent<PlayerItem>();

            if (item == null)
            {
                item =
                    identity.GetComponentInChildren<PlayerItem>(
                        true
                    );
            }

            if (item != null)
            {
                item.SetADSVisualFromServer(
                    isADS
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"[ADS STATE] PlayerIdentity not found. " +
                $"playerId={playerId}"
            );
        }

        bool isCurrentSpectatorTarget =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating &&
            SpectatorManager.Instance.CurrentTargetPlayerId == playerId;

        if (isCurrentSpectatorTarget &&
            InGameUIManager.Instance != null)
        {
            // 관전 중인 대상의 ADS 상태를 크로스헤어에 즉시 반영한다.
            InGameUIManager.Instance.SetDisplayedPlayerADS(isADS);
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                playerId
            );
        }
    }
    private void ClearAllSpikeObjectsForNewRound()
    {
        DroppedSpikePickup[] droppedSpikes =
            FindObjectsByType<DroppedSpikePickup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (DroppedSpikePickup dropped in droppedSpikes)
        {
            if (dropped == null)
                continue;

            Rigidbody rb = dropped.GetComponentInParent<Rigidbody>();

            if (rb != null)
                Destroy(rb.gameObject);
            else
                Destroy(dropped.gameObject);
        }

        Spike[] plantedSpikes =
            FindObjectsByType<Spike>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Spike spike in plantedSpikes)
        {
            if (spike == null)
                continue;

            if (spike.IsLocalInteraction) spike.ForceCancelInteraction();
            Destroy(spike.gameObject);
        }

        Debug.Log("[ROUND RESET] all old spike objects cleared");
    }

    private void ApplySpikeDroppedToPlayer(uint dropperPlayerId)
    {
        PlayerIdentity identity = FindPlayerIdentityById(dropperPlayerId);

        if (identity == null)
        {
            Debug.LogWarning($"[SPIKE DROPPED] dropper identity not found. playerId={dropperPlayerId}");
            return;
        }

        PlayerSpike playerSpike = identity.GetComponent<PlayerSpike>();

        if (playerSpike == null)
            playerSpike = identity.GetComponentInChildren<PlayerSpike>(true);

        if (playerSpike == null)
        {
            Debug.LogWarning($"[SPIKE DROPPED] PlayerSpike missing. playerId={dropperPlayerId}");
            return;
        }

        playerSpike.SetHasSpikeFromServer(false);

        Debug.Log($"[SPIKE DROPPED APPLY] playerId={dropperPlayerId}, hasSpike=false");
    }

    public bool TryGetMyScoreboardMember(out ScoreboardMemberData myData)
    {
        myData = null;

        if (latestScoreboardData == null)
            return false;

        foreach (ScoreboardMemberData member in latestScoreboardData.members)
        {
            if (member.playerId == MyPlayerId)
            {
                myData = member;
                return true;
            }
        }

        return false;
    }

    public void SendWeaponDropRequest(
    int weaponId,
    Vector3 position,
    Vector3 direction)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_WEAPON_DROP_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(weaponId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(direction.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(direction.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(direction.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND WEAPON DROP REQUEST] weaponId={weaponId}, pos={position}, dir={direction}");
    }

    public void SendWeaponPickupRequest(uint dropId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_WEAPON_PICKUP_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(dropId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND WEAPON PICKUP REQUEST] dropId={dropId}");
    }

    private void HandleWeaponDropped(byte[] body)
    {
        const int expectedBodySize = 48;
        if (body == null || body.Length < expectedBodySize)
        {
            Debug.LogError($"[DROP SPAWN FAIL] Invalid packet size. received={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint dropId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint dropperPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        int magAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int reserveAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int skinId = BitConverter.ToInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirX = BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirY = BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirZ = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (WeaponDropSpawner.Instance == null)
        {
            Debug.LogError("[DROP SPAWN FAIL] WeaponDropSpawner.Instance is null");
            return;
        }

        Vector3 pos = new Vector3(x, y, z);
        Vector3 dir = new Vector3(dirX, dirY, dirZ);

        WeaponData data = WeaponDropSpawner.Instance.GetWeaponData(weaponId);
        if (data == null)
        {
            Debug.LogError($"[DROP SPAWN FAIL] weaponData not found. weaponId={weaponId}");
            return;
        }

        GameObject obj = WeaponDropSpawner.Instance.SpawnDroppedWeapon(
            dropId,
            weaponId,
            magAmmo,
            reserveAmmo,
            skinId,
            pos,
            dir
        );

        if (obj == null)
            return;

        RegisterDroppedItem(dropId, obj);

        PlayerIdentity dropperIdentity = FindPlayerIdentityById(dropperPlayerId);

        if (dropperIdentity != null)
        {
            PlayerItem dropperItem = dropperIdentity.GetComponent<PlayerItem>();

            if (dropperItem == null)
                dropperItem = dropperIdentity.GetComponentInChildren<PlayerItem>(true);

            if (dropperItem != null)
            {
                dropperItem.RemoveWeaponFromServer(weaponId);

                Debug.Log(
                    $"[DROP REMOVE HELD WEAPON] dropper={dropperPlayerId}, weaponId={weaponId}"
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[DROP REMOVE HELD WEAPON FAIL] PlayerItem missing. dropper={dropperPlayerId}"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"[DROP REMOVE HELD WEAPON FAIL] PlayerIdentity missing. dropper={dropperPlayerId}"
            );
        }

        Debug.Log(
            $"[DROP SPAWN] dropId={dropId}, " +
            $"dropper={dropperPlayerId}, " +
            $"weaponId={weaponId}, " +
            $"weaponName={data.name}, " +
            $"skinId={skinId}, " +
            $"ammo={magAmmo}/{reserveAmmo}"
        );
    }

    private void HandleWeaponPickupNotify(byte[] body)
    {
        const int expectedBodySize = 24;
        if (body == null || body.Length < expectedBodySize)
        {
            Debug.LogError($"[WEAPON PICKUP NOTIFY] Invalid packet size. received={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint dropId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint pickerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        int magAmmo =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        int reserveAmmo =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        int skinId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        if (dropId != 0)
        {
            RemoveDroppedItem(
                dropId
            );
        }

        if (WeaponDropSpawner.Instance == null)
        {
            Debug.LogError(
                "[WEAPON PICKUP NOTIFY] " +
                "WeaponDropSpawner.Instance is null"
            );

            return;
        }

        WeaponData data =
            WeaponDropSpawner.Instance.GetWeaponData(
                weaponId
            );

        if (data == null)
        {
            Debug.LogError(
                $"[WEAPON PICKUP NOTIFY] " +
                $"WeaponData not found. weaponId={weaponId}"
            );

            return;
        }

        PlayerIdentity pickerIdentity =
            FindPlayerIdentityById(
                pickerPlayerId
            );

        if (pickerIdentity == null)
        {
            Debug.LogWarning(
                $"[WEAPON PICKUP NOTIFY] " +
                $"picker not found. playerId={pickerPlayerId}"
            );

            return;
        }

        PlayerItem pickerItem =
            pickerIdentity.GetComponent<PlayerItem>();

        if (pickerItem == null)
        {
            pickerItem =
                pickerIdentity.GetComponentInChildren<PlayerItem>(
                    true
                );
        }

        if (pickerItem == null)
        {
            Debug.LogWarning(
                $"[WEAPON PICKUP NOTIFY] " +
                $"PlayerItem missing. playerId={pickerPlayerId}"
            );

            return;
        }

        pickerItem.ApplyPickedWeaponFromServer(
            data,
            magAmmo,
            reserveAmmo,
            skinId
        );

        PlayerCombatSnapshot snapshot =
            GetOrCreateCombatSnapshot(
                pickerPlayerId
            );

        snapshot.weaponId = weaponId;
        snapshot.magAmmo = magAmmo;
        snapshot.reserveAmmo = reserveAmmo;
        snapshot.isADS = false;

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                pickerPlayerId
            );
        }

        Debug.Log(
            $"[WEAPON PICKUP APPLY] " +
            $"dropId={dropId}, " +
            $"picker={pickerPlayerId}, " +
            $"weaponId={weaponId}, " +
            $"skinId={skinId}, " +
            $"ammo={magAmmo}/{reserveAmmo}"
        );
    }

    public void SendStoreBuyRequest(byte itemType, int itemId)
    {
        ushort size = 4 + 1 + 4;
        ushort type = (ushort)PacketType.PKT_CS_STORE_BUY_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        buffer[offset] = itemType;
        offset += 1;

        Buffer.BlockCopy(BitConverter.GetBytes(itemId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[STORE BUY REQUEST] itemType={itemType}, itemId={itemId}");
    }

    private void HandleStoreBuyResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        byte itemType = body[offset];
        offset += 1;

        int itemId = BitConverter.ToInt32(body, offset);
        offset += 4;

        int price = BitConverter.ToInt32(body, offset);
        offset += 4;

        int remainCredits = BitConverter.ToInt32(body, offset);
        offset += 4;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        int magAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int reserveAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int barrier = BitConverter.ToInt32(body, offset);
        offset += 4;

        int maxBarrier = BitConverter.ToInt32(body, offset);
        offset += 4;

        byte purchasedSkillMask = body[offset];
        offset += 1;

        byte usedSkillMask = body[offset];
        offset += 1;

        SkillInventoryState.ApplyServerState(
            purchasedSkillMask,
            usedSkillMask
        );

        Debug.Log(
            $"[STORE BUY RESULT] result={result}, itemType={itemType}, itemId={itemId}, " +
            $"price={price}, credits={remainCredits}, weaponId={weaponId}, " +
            $"ammo={magAmmo}/{reserveAmmo}, barrier={barrier}/{maxBarrier}"
        );

        if (StorePanelUI.Instance != null)
        {
            StorePanelUI.Instance.SetCreditDirect(remainCredits);
        }

        if (result != 0)
        {
            Debug.LogWarning($"[STORE BUY FAILED] result={result}");
            return;
        }

        if (StorePanelUI.Instance != null)
        {
            if (itemType == 0)
            {
                // 무기 구매 성공
                StorePanelUI.Instance.SetOwnedWeaponFromServer(weaponId);
            }
            else if (itemType == 1)
            {
                // 방어구 구매 성공
                StorePanelUI.Instance.SetOwnedArmorFromServer(maxBarrier);
            }
        }

        Debug.Log("[STORE BUY SUCCESS]");
    }

    private void HandleSkillState(byte[] body)
    {
        if (body == null || body.Length < 2)
            return;

        byte purchasedSkillMask = body[0];
        byte usedSkillMask = body[1];

        SkillInventoryState.ApplyServerState(
            purchasedSkillMask,
            usedSkillMask
        );

        Debug.Log(
            $"[SKILL STATE] purchased=0x{purchasedSkillMask:X2}, used=0x{usedSkillMask:X2}"
        );
    }

    private byte GetMyTeamForKillLog()
    {
        // Scoreboard에서 받은 내 팀 정보가 가장 정확하다.
        if (latestScoreboardData != null)
            return latestScoreboardData.myTeam;

        PlayerIdentity myIdentity =
            FindPlayerIdentityById(MyPlayerId);

        if (myIdentity != null)
            return (byte)myIdentity.Team;

        Debug.LogWarning(
            "[KILL LOG] local team not found"
        );

        return 255;
    }

    private void RegisterDroppedItem(uint dropId, GameObject obj)
    {
        if (droppedItemMap.TryGetValue(dropId, out GameObject oldObj))
        {
            if (oldObj != null)
                Destroy(oldObj);
        }

        droppedItemMap[dropId] = obj;
    }

    private void RemoveDroppedItem(uint dropId)
    {
        if (!droppedItemMap.TryGetValue(dropId, out GameObject obj))
            return;

        bool destroyedBySpawner =
            WeaponDropSpawner.Instance != null &&
            WeaponDropSpawner.Instance.DestroyDroppedWeapon(dropId);

        if (!destroyedBySpawner && obj != null)
            Destroy(obj);

        droppedItemMap.Remove(dropId);
    }

    private PlayerItem FindMyPlayerItem()
    {
        PlayerIdentity identity = FindPlayerIdentityById(MyPlayerId);

        if (identity == null)
            return null;

        PlayerItem item = identity.GetComponent<PlayerItem>();

        if (item == null)
            item = identity.GetComponentInChildren<PlayerItem>(true);

        return item;
    }

    private PlayerCombatSnapshot GetOrCreateCombatSnapshot(
    uint playerId)
    {
        if (!playerCombatSnapshots.TryGetValue(
            playerId,
            out PlayerCombatSnapshot snapshot))
        {
            snapshot =
                new PlayerCombatSnapshot();

            playerCombatSnapshots[playerId] =
                snapshot;
        }

        return snapshot;
    }

    public bool TryGetCombatSnapshot(
        uint playerId,
        out PlayerCombatSnapshot snapshot)
    {
        return playerCombatSnapshots.TryGetValue(
            playerId,
            out snapshot
        );
    }

    private void HandleFireNotify(byte[] body)
    {
        if (body == null || body.Length < 8)
            return;

        int offset = 0;

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        if (playerId == MyPlayerId)
            return;

        PlayerIdentity identity = FindPlayerIdentityById(playerId);

        if (identity == null)
        {
            Debug.LogWarning($"[FIRE NOTIFY] player not found. playerId={playerId}");
            return;
        }

        PlayerItem playerItem = identity.GetComponent<PlayerItem>();

        if (playerItem == null)
            playerItem = identity.GetComponentInChildren<PlayerItem>(true);

        if (playerItem == null)
        {
            Debug.LogWarning($"[FIRE NOTIFY] PlayerItem not found. playerId={playerId}");
            return;
        }

        playerItem.PlayFireVisualFromServer(weaponId);
    }

    public void SendReloadVisual(
    int weaponId,
    bool isReloading)
    {
        ushort size =
            4 + 4 + 4;

        ushort type = (ushort)PacketType.PKT_CS_RELOAD_VISUAL;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(weaponId),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(
                isReloading ? 1 : 0
            ),
            0,
            buffer,
            offset,
            4
        );

        client.Send(buffer);
    }

    private void HandleReloadVisual(byte[] body)
    {
        if (body == null ||
            body.Length < 12)
        {
            return;
        }

        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(
                body,
                offset
            );
        offset += 4;

        int weaponId =
            BitConverter.ToInt32(
                body,
                offset
            );
        offset += 4;

        bool isReloading =
            BitConverter.ToInt32(
                body,
                offset
            ) != 0;
        offset += 4;

        // 내 화면은 이미 로컬에서 처리했다.
        if (playerId == MyPlayerId)
            return;

        PlayerIdentity identity =
            FindPlayerIdentityById(
                playerId
            );

        if (identity == null)
            return;

        PlayerItem item =
            identity.GetComponent<PlayerItem>();

        if (item == null)
        {
            item =
                identity.GetComponentInChildren<PlayerItem>(
                    true
                );
        }

        if (item == null)
            return;

        item.SetReloadVisualFromServer(
            weaponId,
            isReloading
        );
    }

    private byte GetLocalTeamForRoundResult()
    {
        PlayerIdentity localPlayer =
            FindPlayerIdentityById(MyPlayerId);

        if (localPlayer != null)
            return localPlayer.Team;

        return 255;
    }

    public void SendInGameChat(InGameChatChannel channel, string message)
    {
        message = message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(message))
            return;

        const int messageSize = 256;

        ushort size = 4 + 1 + messageSize;

        ushort type = (ushort)PacketType.PKT_CS_IN_GAME_CHAT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        buffer[offset] = (byte)channel;
        offset += 1;

        Packet.WriteFixedString(buffer, offset, message, messageSize);

        client.Send(buffer);

        Debug.Log($"[SEND IN GAME CHAT] " + $"channel={channel}, message={message}");
    }

    private void HandleInGameChat(byte[] body)
    {
        const int nicknameSize = 20;
        const int messageSize = 256;
        const int requiredBodySize = 4 + 1 + 1 + nicknameSize + messageSize;

        if (body == null || body.Length < requiredBodySize)
        {
            Debug.LogWarning($"[IN GAME CHAT] invalid body length=" + $"{body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint senderPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        InGameChatChannel channel = (InGameChatChannel)body[offset];
        offset += 1;

        byte senderTeam = body[offset];
        offset += 1;

        string nickname = Packet.ReadFixedString(body, offset, nicknameSize);
        offset += nicknameSize;

        string message = Packet.ReadFixedString(body, offset, messageSize);
        offset += messageSize;

        if (InGameChatUI.Instance != null)
        {
            InGameChatUI.Instance.OnReceiveChat(senderPlayerId, channel, senderTeam, nickname, message);
        }
        else
        {
            Debug.LogWarning(
                "[IN GAME CHAT] " +
                "InGameChatUI.Instance is null"
            );
        }
    }

    public void SendDroneDeploy(
    float x,
    float y,
    float z,
    float yaw,
    float pitch)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_DEPLOY;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(yaw), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(pitch), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND DRONE DEPLOY] pos=({x}, {y}, {z}), yaw={yaw}, pitch={pitch}");
    }

    public void SendDroneState(
        uint droneId,
        float x,
        float y,
        float z,
        float yaw,
        float pitch)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_STATE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(droneId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(yaw), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(pitch), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);
    }

    public void SendDroneExit(uint droneId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_EXIT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(droneId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND DRONE EXIT] droneId={droneId}");
    }

    private void HandleDroneSpawn(byte[] body)
    {
        if (body == null || body.Length < 28)
        {
            Debug.LogWarning($"[DRONE SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float yaw = BitConverter.ToSingle(body, offset);
        offset += 4;

        float pitch = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (DroneNetworkManager.Instance == null)
        {
            Debug.LogError("[DRONE SPAWN] DroneNetworkManager.Instance is null");
            return;
        }

        DroneNetworkManager.Instance.OnDroneSpawn(
            droneId,
            ownerPlayerId,
            x,
            y,
            z,
            yaw,
            pitch
        );



        Debug.Log($"[DRONE SPAWN] droneId={droneId}, owner={ownerPlayerId}");
    }

    private void HandleDroneState(byte[] body)
    {
        if (body == null || body.Length < 28)
            return;

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float yaw = BitConverter.ToSingle(body, offset);
        offset += 4;

        float pitch = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (DroneNetworkManager.Instance == null)
            return;

        DroneNetworkManager.Instance.OnDroneState(
            droneId,
            ownerPlayerId,
            x,
            y,
            z,
            yaw,
            pitch
        );
    }

    private void HandleDroneDestroy(byte[] body)
    {
        if (body == null || body.Length < 9)
        {
            Debug.LogWarning($"[DRONE DESTROY] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte reason = body[offset];
        offset += 1;

        if (DroneNetworkManager.Instance == null)
        {
            Debug.LogError("[DRONE DESTROY] DroneNetworkManager.Instance is null");
            return;
        }

        DroneNetworkManager.Instance.OnDroneDestroy(
            droneId,
            ownerPlayerId,
            reason
        );

        Debug.Log($"[DRONE DESTROY] droneId={droneId}, owner={ownerPlayerId}, reason={reason}");
    }

    public void SendDroneHit(uint droneId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_HIT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(droneId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND DRONE HIT] droneId={droneId}");
    }

    public void SendDroneTaserFire(
    uint droneId,
    uint targetPlayerId)
    {
        ushort size = 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_TASER_FIRE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(droneId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(targetPlayerId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND DRONE TASER FIRE] " +
            $"droneId={droneId}, targetPlayerId={targetPlayerId}"
        );
    }

    private void HandleDroneTaserFire(byte[] body)
    {
        const int requiredSize = 4 + 4 + 4 + 1 + 1 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[DRONE TASER FIRE] invalid body length={body?.Length ?? 0}"
            );
            return;
        }

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint targetPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte hit = body[offset];
        offset += 1;

        byte stack = body[offset];
        offset += 1;

        float duration = BitConverter.ToSingle(body, offset);
        offset += 4;

        int damage = BitConverter.ToInt32(body, offset);
        offset += 4;

        Debug.Log(
            $"[DRONE TASER FIRE] droneId={droneId}, " +
            $"owner={ownerPlayerId}, target={targetPlayerId}, " +
            $"hit={hit}, stack={stack}, duration={duration}, damage={damage}"
        );

        bool isMyDroneTaser =
            ownerPlayerId == MyPlayerId;

        bool successHit =
            hit != 0 &&
            targetPlayerId != 0;

        if (DroneNetworkManager.Instance != null &&
            DroneNetworkManager.Instance.TryGetActiveDroneByOwner(ownerPlayerId, out var firingDrone) &&
            firingDrone != null && firingDrone.DroneId == droneId)
            firingDrone.TaserGun?.PlayServerResult(successHit, successHit ? FindPlayerIdentityById(targetPlayerId)?.transform : null);

        // 핵심:
        // 서버 판정 결과를 내 드론 HUD에 반영
        if (isMyDroneTaser && DroneHUDUI.Instance != null)
        {
            if (successHit)
            {
                DroneHUDUI.Instance.PlayTaserHitConfirm();
            }
            else
            {
                DroneHUDUI.Instance.PlayTaserMiss();
            }
        }

        // 빗나감이면 HUD 피드백만 주고 종료
        if (!successHit)
            return;

        PlayerIdentity targetIdentity =
            FindPlayerIdentityById(targetPlayerId);

        if (targetIdentity == null)
        {
            Debug.LogWarning(
                $"[DRONE TASER FIRE] target player not found. " +
                $"targetPlayerId={targetPlayerId}"
            );
            return;
        }

        ElectricShock shock =
            targetIdentity.GetComponent<ElectricShock>();

        if (shock == null)
            shock = targetIdentity.GetComponentInChildren<ElectricShock>(true);

        if (shock == null)
        {
            Debug.LogWarning(
                $"[DRONE TASER FIRE] ElectricShock missing. " +
                $"targetPlayerId={targetPlayerId}"
            );
            return;
        }

        shock.Shock(duration);

        Debug.Log(
            $"[DRONE TASER APPLY] target={targetPlayerId}, " +
            $"duration={duration}, stack={stack}, damage={damage}"
        );

        // 나중에 ElectricShock에 스택 함수 만들면 이걸로 교체
        // shock.ApplyStack(stack, duration);
    }

    private void HandleDroneHpUpdate(byte[] body)
    {
        const int requiredSize = 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[DRONE HP UPDATE] invalid body size. " +
                $"size={(body == null ? 0 : body.Length)}"
            );
            return;
        }

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int hp = BitConverter.ToInt32(body, offset);
        offset += 4;

        int maxHp = BitConverter.ToInt32(body, offset);
        offset += 4;

        if (DroneNetworkManager.Instance != null)
        {
            DroneNetworkManager.Instance.OnDroneHpUpdate(
                droneId,
                ownerPlayerId,
                hp,
                maxHp
            );
        }
    }

    public void SendScannerThrow(
    float x,
    float y,
    float z,
    float vx,
    float vy,
    float vz)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SCANNER_THROW;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(vx), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(vy), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(vz), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND SCANNER THROW] " +
            $"pos=({x},{y},{z}), velocity=({vx},{vy},{vz})"
        );
    }

    public void SendScannerAnchor(
        uint scannerId,
        float x,
        float y,
        float z)
    {
        ushort size = 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SCANNER_ANCHOR;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(scannerId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND SCANNER ANCHOR] " +
            $"scannerId={scannerId}, pos=({x},{y},{z})"
        );
    }

    private void HandleScannerSpawn(byte[] body)
    {
        const int requiredSize = 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning($"[SCANNER SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint scannerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float vx = BitConverter.ToSingle(body, offset);
        offset += 4;

        float vy = BitConverter.ToSingle(body, offset);
        offset += 4;

        float vz = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (ScannerNetworkManager.Instance != null)
        {
            ScannerNetworkManager.Instance.OnScannerSpawn(
                scannerId,
                ownerPlayerId,
                x,
                y,
                z,
                vx,
                vy,
                vz
            );
        }
    }

    private void HandleScannerAnchored(byte[] body)
    {
        const int requiredSize = 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint scannerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (ScannerNetworkManager.Instance != null)
        {
            ScannerNetworkManager.Instance.OnScannerAnchored(
                scannerId,
                x,
                y,
                z
            );
        }
    }

    private void HandleScannerZap(byte[] body)
    {
        const int requiredSize = 4 + 4 + 1 + 4;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint scannerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint targetPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte stack = body[offset];
        offset += 1;

        float duration = BitConverter.ToSingle(body, offset);
        offset += 4;

        // Older servers send the original 13-byte body. Newer servers include
        // the exact endpoint used for wall occlusion, including crouch height.
        Vector3? confirmedTarget = null;
        if (body.Length >= requiredSize + 12)
            confirmedTarget = new Vector3(BitConverter.ToSingle(body, offset),
                BitConverter.ToSingle(body, offset + 4), BitConverter.ToSingle(body, offset + 8));

        if (ScannerNetworkManager.Instance != null)
        {
            ScannerNetworkManager.Instance.OnScannerZap(
                scannerId,
                targetPlayerId,
                stack,
                duration, confirmedTarget
            );
        }
    }

    private void HandleScannerDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
            return;

        uint scannerId = BitConverter.ToUInt32(body, 0);

        if (ScannerNetworkManager.Instance != null)
        {
            ScannerNetworkManager.Instance.OnScannerDestroy(scannerId,body.Length>=5 && body[4]==1);
        }
    }

    public void SendScannerHit(uint scannerId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SCANNER_HIT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(scannerId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SCANNER HIT] scannerId={scannerId}");
    }

    public void SendEmpCast()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_EMP_CAST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        client.Send(buffer);

        Debug.Log("[SEND EMP CAST]");
    }

    public void SendEmpGesture(bool preparing)
    {
        if(!IsConnected || !IsAuthenticated)return;
        var buffer=new byte[5];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)5),0,buffer,0,2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_EMP_GESTURE),0,buffer,2,2);
        buffer[4]=(byte)(preparing?1:0);client.Send(buffer);
    }
    public void SendScannerGesture(byte phase)
    {
        SendSkillGesture(PacketType.PKT_CS_SCANNER_GESTURE, phase);
    }
    public void SendSentinelGesture(byte phase)=>SendSkillGesture(PacketType.PKT_CS_SENTINEL_GESTURE,phase);
    public void SendDischargeGesture(bool preparing) => SendSkillGesture(PacketType.PKT_CS_DISCHARGE_GESTURE, (byte)(preparing ? 1 : 0));
    private void SendSkillGesture(PacketType type, byte phase)
    {
        if(!IsConnected || !IsAuthenticated || phase>2)return;
        var buffer=new byte[5];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)5),0,buffer,0,2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)type),0,buffer,2,2);
        buffer[4]=phase;client.Send(buffer);
    }

    private void HandleEmpCast(byte[] body)
    {
        if(body==null || body.Length<5 || body[4]>8 || body.Length<5+body[4]*4)return;
        int offset = 0;

        uint casterPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte targetCount = body[offset];
        offset += 1;

        uint[] targetPlayerIds = new uint[targetCount];

        for (int i = 0; i < targetCount; i++)
        {
            targetPlayerIds[i] =
                BitConverter.ToUInt32(body, offset);
            offset += 4;
        }

        PlayerIdentity caster =
            FindPlayerIdentityById(casterPlayerId);

        if (caster == null)
            return;

        InitiatorSkill_E skill =
            caster.GetComponent<InitiatorSkill_E>();

        if (skill == null)
            return;

        skill.PlayEmpEffect(targetPlayerIds);

        Debug.Log(
            $"[EMP CAST APPLY] caster={casterPlayerId}, targetCount={targetCount}"
        );
    }

    private void HandleElectricStatus(byte[] body)
    {
        if(body==null || body.Length<10)return;
        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte shockStack = body[offset];
        offset += 1;

        byte stunned = body[offset];
        offset += 1;

        float stunSeconds =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        if(float.IsNaN(stunSeconds) || float.IsInfinity(stunSeconds) || stunSeconds<0)return;

        PlayerIdentity target =
            FindPlayerIdentityById(playerId);

        if (target == null)
        {
            Debug.LogWarning($"[ELECTRIC STATUS] target not found playerId={playerId}");
            return;
        }

        ElectricShock shock =
            target.GetComponent<ElectricShock>();

        if (shock == null)
            shock = target.GetComponentInChildren<ElectricShock>(true);

        if (shock != null)
        {
            shock.ApplyServerStatus(
                shockStack,
                stunned != 0,
                stunSeconds
            );
        }
        else
        {
            Debug.LogWarning($"[ELECTRIC STATUS] ElectricShock missing playerId={playerId}");
        }

        PlayerMovement movement =
            target.GetComponent<PlayerMovement>();

        if (movement == null)
            movement = target.GetComponentInChildren<PlayerMovement>(true);

        if (movement != null)
        {
            if (stunned != 0)
            {
                movement.ApplyStunFromServer(stunSeconds);
            }
            else if (shockStack == 0)
            {
                movement.ClearStunFromServer();
            }
        }
        else
        {
            Debug.LogWarning($"[ELECTRIC STATUS] PlayerMovement missing playerId={playerId}");
        }

        Debug.Log(
            $"[ELECTRIC STATUS APPLY] playerId={playerId}, " +
            $"stack={shockStack}, stunned={stunned}, stun={stunSeconds}"
        );
    }

    public void SendUltimateCast(Vector3 direction)
    {
        if (!IsConnected || !IsAuthenticated) return;
        ushort size = 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_ULTIMATE_CAST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Vector3 dir =
            direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;

        Buffer.BlockCopy(
            BitConverter.GetBytes(dir.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(dir.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(dir.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ULTIMATE CAST] dir={dir}"
        );
    }

    private void HandleUltimateCast(byte[] body)
    {
        if (body == null || body.Length < 46 || body[16] > 6) return;
        const int maxTargetCount = 6;

        int offset = 0;

        uint casterPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        float dirX =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirY =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirZ =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        byte targetCount =
            body[offset];
        offset += 1;

        uint[] targetPlayerIds =
            new uint[targetCount];

        for (int i = 0; i < maxTargetCount; i++)
        {
            uint targetPlayerId =
                BitConverter.ToUInt32(body, offset);
            offset += 4;

            if (i < targetCount)
            {
                targetPlayerIds[i] =
                    targetPlayerId;
            }
        }

        int damage =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        byte stack =
            body[offset];
        offset += 1;

        PlayerIdentity caster =
            FindPlayerIdentityById(casterPlayerId);

        if (caster == null)
        {
            Debug.LogWarning(
                $"[ULTIMATE CAST] caster not found. caster={casterPlayerId}"
            );

            return;
        }

        InitiatorSkill_X skill =
            caster.GetComponent<InitiatorSkill_X>();

        if (skill == null)
            skill = caster.GetComponentInChildren<InitiatorSkill_X>(true);

        if (skill != null)
        {
            skill.PlayUltimateEffect(
                targetPlayerIds,
                new Vector3(dirX, dirY, dirZ)
            );
        }

        Debug.Log(
            $"[ULTIMATE CAST APPLY] caster={casterPlayerId}, " +
            $"targetCount={targetCount}, damage={damage}, stack={stack}"
        );
    }

    public void SendBeastStealthCast()
    {
        ushort size = 4;
        ushort type =
            (ushort)PacketType.PKT_CS_BEAST_STEALTH_CAST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );

        client.Send(buffer);

        Debug.Log("[SEND BEAST STEALTH CAST]");
    }

    public void SendBeastStealthCancel()
    {
        ushort size = 4;
        ushort type =
            (ushort)PacketType.PKT_CS_BEAST_STEALTH_CANCEL;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );

        client.Send(buffer);

        Debug.Log("[SEND BEAST STEALTH CANCEL]");
    }

    private void HandleBeastStealthState(byte[] body)
    {
        const int requiredSize = 4 + 1 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[BEAST STEALTH STATE] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        bool active =
            body[offset] != 0;
        offset += 1;

        float duration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float speedMultiplier =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float revealRange =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        PlayerIdentity target =
            FindPlayerIdentityById(playerId);

        if (target == null)
        {
            Debug.LogWarning(
                $"[BEAST STEALTH STATE] player not found. playerId={playerId}"
            );

            return;
        }

        EntrySkill_E skill =
            target.GetComponent<EntrySkill_E>();

        if (skill == null)
            skill = target.GetComponentInChildren<EntrySkill_E>(true);

        if (skill == null)
        {
            Debug.LogWarning(
                $"[BEAST STEALTH STATE] EntrySkill_E missing. playerId={playerId}"
            );

            return;
        }

        skill.ApplyStealthStateFromServer(
            active,
            duration,
            speedMultiplier,
            revealRange
        );

        Debug.Log(
            $"[BEAST STEALTH STATE APPLY] playerId={playerId}, " +
            $"active={active}, duration={duration}, " +
            $"speedMultiplier={speedMultiplier}, revealRange={revealRange}"
        );
    }

    public void SendEntrySmokeThrow(
    Vector3 position,
    Vector3 velocity)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_SMOKE_THROW;

        byte[] buffer =
            new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ENTRY SMOKE THROW] pos={position}, velocity={velocity}"
        );
    }

    public void SendEntrySmokeAttach(
        uint smokeId,
        Vector3 position,
        Vector3 normal)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_SMOKE_ATTACH;

        byte[] buffer =
            new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(smokeId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ENTRY SMOKE ATTACH] smokeId={smokeId}, pos={position}, normal={normal}"
        );
    }

    private void HandleEntrySmokeSpawn(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY SMOKE SPAWN] invalid body length={body?.Length ?? 0}"
            );
            return;
        }

        int offset = 0;

        uint smokeId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float y =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float z =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float vx =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float vy =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float vz =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        if (EntrySmokeNetworkManager.Instance != null)
        {
            var entry = FindPlayerIdentityById(ownerPlayerId)?.GetComponent<EntrySkillPresentation>();
            if (entry && !entry.IsLocal) entry.Begin(EntrySkillPresentation.Cast.Smoke, .35f);
            EntrySmokeNetworkManager.Instance.OnSmokeSpawn(
                smokeId,
                ownerPlayerId,
                new Vector3(x, y, z),
                new Vector3(vx, vy, vz)
            );
        }
    }

    private void HandleEntrySmokeActivate(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY SMOKE ACTIVATE] invalid body length={body?.Length ?? 0}"
            );
            return;
        }

        int offset = 0;

        uint smokeId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float y =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float z =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float nx =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float ny =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float nz =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float radius =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float duration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        if (EntrySmokeNetworkManager.Instance != null)
        {
            EntrySmokeNetworkManager.Instance.OnSmokeActivate(
                smokeId,
                ownerPlayerId,
                new Vector3(x, y, z),
                new Vector3(nx, ny, nz),
                radius,
                duration
            );
        }
    }

    private void HandleEntrySmokeDestroy(byte[] body)
    {
        if (body == null ||
            body.Length < 4)
            return;

        uint smokeId =
            BitConverter.ToUInt32(body, 0);

        if (EntrySmokeNetworkManager.Instance != null)
        {
            EntrySmokeNetworkManager.Instance.OnSmokeDestroy(
                smokeId
            );
        }
    }

    public void SendEntrySmokeState(
    uint smokeId,
    Vector3 position,
    Quaternion rotation)
    {
        // header 4
        // smokeId 4
        // position 12
        // rotation 16
        // total = 36
        ushort size = 4 + 4 + 12 + 16;
        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_SMOKE_STATE;

        byte[] buffer =
            new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(smokeId),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(position.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(position.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(position.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(rotation.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(rotation.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(rotation.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(rotation.w),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ENTRY SMOKE STATE] smokeId={smokeId}, pos={position}"
        );
    }
    private void HandleEntrySmokeState(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY SMOKE STATE] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint smokeId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float rotX = BitConverter.ToSingle(body, offset);
        offset += 4;

        float rotY = BitConverter.ToSingle(body, offset);
        offset += 4;

        float rotZ = BitConverter.ToSingle(body, offset);
        offset += 4;

        float rotW = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (EntrySmokeNetworkManager.Instance != null)
        {
            EntrySmokeNetworkManager.Instance.OnSmokeState(
                smokeId,
                ownerPlayerId,
                new Vector3(x, y, z),
                new Quaternion(rotX, rotY, rotZ, rotW)
            );
        }
    }

    public void SendEntryDashCast(Vector3 direction)
    {
        ushort size =
            4 + 4 + 4 + 4;

        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_DASH_CAST;

        byte[] buffer =
            new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ENTRY DASH CAST] dir={direction}"
        );
    }

    private void HandleEntryDashCast(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY DASH CAST] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        float dirX =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirY =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirZ =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float dashForce =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float dashDuration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity == null)
        {
            Debug.LogWarning(
                $"[ENTRY DASH CAST] player not found playerId={playerId}"
            );

            return;
        }

        EntryDashMotor dashMotor =
            identity.GetComponent<EntryDashMotor>();

        if (dashMotor == null)
            dashMotor =
                identity.GetComponentInChildren<EntryDashMotor>(true);

        if (dashMotor == null)
            dashMotor =
                identity.gameObject.AddComponent<EntryDashMotor>();

        dashMotor.PlayDash(
            new Vector3(dirX, dirY, dirZ),
            dashForce,
            dashDuration
        );

        Debug.Log(
            $"[ENTRY DASH CAST APPLY] playerId={playerId}, " +
            $"dir=({dirX}, {dirY}, {dirZ}), force={dashForce}, duration={dashDuration}"
        );
    }

    private void HandleEntryDashEnd(byte[] body)
    {
        const int requiredSize = 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY DASH END] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        uint playerId =
            BitConverter.ToUInt32(body, 0);

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity == null)
            return;

        EntryDashMotor dashMotor =
            identity.GetComponent<EntryDashMotor>();

        if (dashMotor == null)
            dashMotor =
                identity.GetComponentInChildren<EntryDashMotor>(true);

        if (dashMotor != null)
        {
            dashMotor.ForceEndDash();
        }

        Debug.Log(
            $"[ENTRY DASH END APPLY] playerId={playerId}"
        );
    }

    public void SendEntryBladeActivate()
    {
        ushort size = 4;
        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_BLADE_ACTIVATE;

        byte[] buffer =
            new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        client.Send(buffer);

        Debug.Log("[SEND ENTRY BLADE ACTIVATE]");
    }

    public void SendEntryBladeFire(
        Vector3 direction,
        uint targetPlayerId,
        Vector3 hitPoint,
        bool burst = false)
    {
        ushort size =
            4 + 12 + 4 + 12; // header + dir + targetId + hitPoint = 32

        ushort type =
            (ushort)(burst ? PacketType.PKT_CS_ENTRY_BLADE_BURST : PacketType.PKT_CS_ENTRY_BLADE_FIRE);

        byte[] buffer =
            new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(targetPlayerId),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(hitPoint.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(hitPoint.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(hitPoint.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ENTRY BLADE FIRE] target={targetPlayerId}, hitPoint={hitPoint}"
        );
    }

    private void HandleEntryBladeState(byte[] body)
    {
        const int requiredSize =
            4 + 1 + 1 + 4;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE STATE] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        bool active =
            body[offset] != 0;
        offset += 1;

        byte bladeCount =
            body[offset];
        offset += 1;

        float duration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity == null)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE STATE] player not found. playerId={playerId}"
            );

            return;
        }

        EntrySkill_X skill =
            identity.GetComponent<EntrySkill_X>();

        if (skill == null)
        {
            skill =
                identity.GetComponentInChildren<EntrySkill_X>(true);
        }

        if (skill == null)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE STATE] EntrySkill_X missing. playerId={playerId}"
            );

            return;
        }

        skill.ApplyBladeStateFromServer(
            active,
            bladeCount,
            duration
        );

        Debug.Log(
            $"[ENTRY BLADE STATE APPLY] playerId={playerId}, active={active}, blades={bladeCount}, duration={duration}"
        );
    }

    private void HandleEntryBladeFire(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 1 + 1 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE FIRE] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint shooterPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint targetPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        bool hit =
            body[offset] != 0;
        offset += 1;

        byte remainingBlades =
            body[offset];
        offset += 1;

        float startX =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float startY =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float startZ =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float endX =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float endY =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float endZ =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        PlayerIdentity shooter =
            FindPlayerIdentityById(shooterPlayerId);

        if (shooter == null)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE FIRE] shooter not found. shooter={shooterPlayerId}"
            );

            return;
        }

        EntrySkill_X skill =
            shooter.GetComponent<EntrySkill_X>();

        if (skill == null)
        {
            skill =
                shooter.GetComponentInChildren<EntrySkill_X>(true);
        }

        if (skill == null)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE FIRE] EntrySkill_X missing. shooter={shooterPlayerId}"
            );

            return;
        }

        skill.PlayBladeFireEffect(
            new Vector3(startX, startY, startZ),
            new Vector3(endX, endY, endZ),
            hit,
            remainingBlades
        );

        Debug.Log(
            $"[ENTRY BLADE FIRE APPLY] shooter={shooterPlayerId}, target={targetPlayerId}, hit={hit}, remaining={remainingBlades}"
        );
    }

    public void SendControllerSwampThrow(
    Vector3 position,
    Vector3 velocity)
    {
        ushort size =
            4 + 12 + 12;

        ushort type =
            (ushort)PacketType.PKT_CS_CONTROLLER_SWAMP_THROW;

        byte[] buffer =
            new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND CONTROLLER SWAMP THROW] pos={position}, velocity={velocity}"
        );
    }

    public void SendControllerSwampActivate(uint trapId, Vector3 position, Vector3 normal)
    {
        ushort size = 4 + 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_SWAMP_ACTIVATE;
        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(normal.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(normal.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);
        Debug.Log($"[SEND CONTROLLER SWAMP ACTIVATE] trapId={trapId}, pos={position}, normal={normal}");
    }

    private void HandleControllerSwampSpawn(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 12 + 12;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[CONTROLLER SWAMP SPAWN] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint trapId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float y =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float z =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float vx =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float vy =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float vz =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        if (ControllerSwampNetworkManager.Instance != null)
        {
            ControllerSwampNetworkManager.Instance.OnSwampSpawn(
                trapId,
                ownerPlayerId,
                new Vector3(x, y, z),
                new Vector3(vx, vy, vz)
            );
        }
        else
        {
            Debug.LogError(
                "[CONTROLLER SWAMP SPAWN] ControllerSwampNetworkManager.Instance is null"
            );
        }
    }

    private void HandleControllerSwampActivate(byte[] body)
    {
        const int requiredSize = 40;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning($"[SWAMP ACTIVATE] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        float normalX = BitConverter.ToSingle(body, offset); offset += 4;
        float normalY = BitConverter.ToSingle(body, offset); offset += 4;
        float normalZ = BitConverter.ToSingle(body, offset); offset += 4;

        float radius = BitConverter.ToSingle(body, offset); offset += 4;
        float duration = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 position = new Vector3(x, y, z);
        Vector3 normal = new Vector3(normalX, normalY, normalZ);

        Debug.Log($"[RECV SWAMP ACTIVATE] trapId={trapId}, owner={ownerPlayerId}, pos={position}, normal={normal}, radius={radius}, duration={duration}");

        if (ControllerSwampNetworkManager.Instance != null)
        {
            ControllerSwampNetworkManager.Instance.OnSwampActivate(trapId, ownerPlayerId, position, normal, radius, duration);
        }
    }

    private void HandleControllerSwampDestroy(byte[] body)
    {
        if (body == null ||
            body.Length < 4)
        {
            return;
        }

        uint trapId =
            BitConverter.ToUInt32(body, 0);

        if (ControllerSwampNetworkManager.Instance != null)
        {
            ControllerSwampNetworkManager.Instance.OnSwampDestroy(
                trapId
            );
        }
    }

    public void SendControllerSmokeDrop(Vector3 position)
    {
        ushort size = 4 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_SMOKE_DROP;
        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);
        Debug.Log($"[SEND CONTROLLER SMOKE DROP] pos={position}");
    }

    private void HandleControllerSmokeSpawn(byte[] body)
    {
        if (body == null || body.Length < 28)
        {
            Debug.LogWarning($"[CONTROLLER SMOKE SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint smokeId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        float radius = BitConverter.ToSingle(body, offset); offset += 4;
        float duration = BitConverter.ToSingle(body, offset); offset += 4;

        if (ControllerSmokeNetworkManager.Instance != null)
        {
            ControllerSmokeNetworkManager.Instance.OnSmokeSpawn(smokeId, ownerPlayerId, new Vector3(x, y, z), radius, duration);
        }
    }

    private void HandleControllerSmokeDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
        {
            Debug.LogWarning($"[CONTROLLER SMOKE DESTROY] invalid body length={body?.Length ?? 0}");
            return;
        }

        uint smokeId = BitConverter.ToUInt32(body, 0);

        if (ControllerSmokeNetworkManager.Instance != null)
        {
            ControllerSmokeNetworkManager.Instance.OnSmokeDestroy(smokeId);
        }
    }

    public void SendControllerDecoyThrow(Vector3 position, Vector3 velocity)
    {
        ushort size = 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_DECOY_THROW;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(velocity.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(velocity.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND CONTROLLER DECOY THROW] pos={position}, velocity={velocity}");
    }

    public void SendControllerDecoyActivate(uint decoyId, Vector3 position, Vector3 normal)
    {
        ushort size = 4 + 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_DECOY_ACTIVATE;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(decoyId), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(normal.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(normal.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND CONTROLLER DECOY ACTIVATE] decoyId={decoyId}, pos={position}, normal={normal}");
    }

    public void SendControllerDecoyTrigger(uint decoyId)
    {
        if (!IsConnected) return;
        byte[] buffer = new byte[8];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)8), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_CONTROLLER_DECOY_TRIGGER), 0, buffer, 2, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(decoyId), 0, buffer, 4, 4);
        client.Send(buffer);
    }

    private void HandleControllerDecoySpawn(byte[] body)
    {
        if (body == null || body.Length < 32)
        {
            Debug.LogWarning($"[RECV CONTROLLER DECOY SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint decoyId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float px = BitConverter.ToSingle(body, offset); offset += 4;
        float py = BitConverter.ToSingle(body, offset); offset += 4;
        float pz = BitConverter.ToSingle(body, offset); offset += 4;

        float vx = BitConverter.ToSingle(body, offset); offset += 4;
        float vy = BitConverter.ToSingle(body, offset); offset += 4;
        float vz = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 position = new Vector3(px, py, pz);
        Vector3 velocity = new Vector3(vx, vy, vz);

        if (ControllerDecoyNetworkManager.Instance != null)
        {
            ControllerDecoyNetworkManager.Instance.OnDecoySpawn(
                decoyId,
                ownerPlayerId,
                position,
                velocity
            );
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER DECOY SPAWN] ControllerDecoyNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER DECOY SPAWN] decoyId={decoyId}, owner={ownerPlayerId}, pos={position}, velocity={velocity}");
    }

    private void HandleControllerDecoyActivate(byte[] body)
    {
        if (body == null || body.Length < 36)
        {
            Debug.LogWarning($"[RECV CONTROLLER DECOY ACTIVATE] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint decoyId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float px = BitConverter.ToSingle(body, offset); offset += 4;
        float py = BitConverter.ToSingle(body, offset); offset += 4;
        float pz = BitConverter.ToSingle(body, offset); offset += 4;

        float nx = BitConverter.ToSingle(body, offset); offset += 4;
        float ny = BitConverter.ToSingle(body, offset); offset += 4;
        float nz = BitConverter.ToSingle(body, offset); offset += 4;

        float duration = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 position = new Vector3(px, py, pz);
        Vector3 normal = new Vector3(nx, ny, nz);

        if (ControllerDecoyNetworkManager.Instance != null)
        {
            ControllerDecoyNetworkManager.Instance.OnDecoyActivate(
                decoyId,
                ownerPlayerId,
                position,
                normal,
                duration
            );
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER DECOY ACTIVATE] ControllerDecoyNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER DECOY ACTIVATE] decoyId={decoyId}, owner={ownerPlayerId}, pos={position}, normal={normal}, duration={duration}");
    }

    private void HandleControllerDecoyDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
        {
            Debug.LogWarning($"[RECV CONTROLLER DECOY DESTROY] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint decoyId = BitConverter.ToUInt32(body, offset); offset += 4;

        if (ControllerDecoyNetworkManager.Instance != null)
        {
            ControllerDecoyNetworkManager.Instance.OnDecoyDestroy(decoyId);
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER DECOY DESTROY] ControllerDecoyNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER DECOY DESTROY] decoyId={decoyId}");
    }

    public void SendControllerUltimateCast(Vector3 position)
    {
        ushort size = 4 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_ULTIMATE_CAST;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND CONTROLLER ULTIMATE CAST] pos={position}");
    }

    private void HandleControllerUltimateSpawn(byte[] body)
    {
        if (body == null || body.Length < 28)
        {
            Debug.LogWarning($"[RECV CONTROLLER ULTIMATE SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint ultimateId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        float radius = BitConverter.ToSingle(body, offset); offset += 4;
        float duration = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 position = new Vector3(x, y, z);

        if (ControllerUltimateNetworkManager.Instance != null)
        {
            ControllerUltimateNetworkManager.Instance.OnUltimateSpawn(
                ultimateId,
                ownerPlayerId,
                position,
                radius,
                duration
            );
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER ULTIMATE SPAWN] ControllerUltimateNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER ULTIMATE SPAWN] ultimateId={ultimateId}, owner={ownerPlayerId}, pos={position}, radius={radius}, duration={duration}");
    }

    private void HandleControllerUltimateDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
        {
            Debug.LogWarning($"[RECV CONTROLLER ULTIMATE DESTROY] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint ultimateId = BitConverter.ToUInt32(body, offset); offset += 4;

        if (ControllerUltimateNetworkManager.Instance != null)
        {
            ControllerUltimateNetworkManager.Instance.OnUltimateDestroy(ultimateId);
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER ULTIMATE DESTROY] ControllerUltimateNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER ULTIMATE DESTROY] ultimateId={ultimateId}");
    }

    public void SendSentinelCThrow(Vector3 position, Vector3 velocity)
    {
        ushort size = 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_C_THROW;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL C THROW] pos={position}, velocity={velocity}");
    }

    public void SendSentinelCAnchor(uint trapId, Vector3 position, Vector3 normal)
    {
        ushort size = 4 + 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_C_ANCHOR;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL C ANCHOR] trapId={trapId}, pos={position}, normal={normal}");
    }

    public void SendSentinelCTrigger(uint trapId, Vector3 position, float radius, float revealDuration)
    {
        ushort size = 4 + 4 + 12 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_C_TRIGGER;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(radius), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(revealDuration), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL C TRIGGER] trapId={trapId}, pos={position}, radius={radius}");
    }

    public void SendSentinelCHit(uint trapId, float damage)
    {
        ushort size = 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_C_HIT;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(damage), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL C HIT] trapId={trapId}, damage={damage}");
    }

    private void HandleSentinelCSpawn(byte[] body)
    {
        if (body == null || body.Length < 37)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        Vector3 velocity = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float hp = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnSpawn(
                trapId,
                ownerPlayerId,
                ownerTeam,
                position,
                velocity,
                hp
            );
        }
    }

    private void HandleSentinelCAnchor(byte[] body)
    {
        if (body == null || body.Length < 28)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        Vector3 normal = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        if (SentinelCNetworkManager.Instance != null)
            SentinelCNetworkManager.Instance.OnAnchor(trapId, position, normal);
    }

    private void HandleSentinelCTrigger(byte[] body)
    {
        if (body == null || body.Length < 29)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float radius = BitConverter.ToSingle(body, offset);
        offset += 4;

        float revealDuration = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnTrigger(
                trapId,
                ownerPlayerId,
                ownerTeam,
                position,
                radius,
                revealDuration
            );
        }
    }

    private void HandleSentinelCDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
            return;

        uint trapId = BitConverter.ToUInt32(body, 0);

        if (SentinelCNetworkManager.Instance != null)
            SentinelCNetworkManager.Instance.OnDestroyTrap(trapId);
    }

    public void SendSentinelQPlace(Vector3 position)
    {
        ushort size = 4 + 12;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_Q_PLACE;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL Q PLACE] position={position}");
    }

    public void SendSentinelQActivate(uint trapId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_Q_ACTIVATE;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL Q ACTIVATE] trapId={trapId}");
    }

    public void SendSentinelQHit(uint trapId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_Q_HIT;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL Q HIT] trapId={trapId}");
    }

    private void HandleSentinelQSpawn(byte[] body)
    {
        const int requiredSize = 33;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float hp = BitConverter.ToSingle(body, offset);
        offset += 4;

        float detectionRadius = BitConverter.ToSingle(body, offset);
        offset += 4;

        float zoneRadius = BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnQSpawn(
                trapId,
                ownerPlayerId,
                ownerTeam,
                position,
                hp,
                detectionRadius,
                zoneRadius
            );
        }
    }

    private void HandleSentinelQActivate(byte[] body)
    {
        const int requiredSize = 12;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float duration = BitConverter.ToSingle(body, offset);
        offset += 4;

        float slowMultiplier = BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnQActivate(
                trapId,
                duration,
                slowMultiplier
            );
        }
    }

    private void HandleSentinelQDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
            return;

        uint trapId = BitConverter.ToUInt32(body, 0);

        if (SentinelCNetworkManager.Instance != null)
            SentinelCNetworkManager.Instance.OnQDestroy(trapId);
    }

    public void SendSentinelEPlace(
        Vector3 position,
        Vector3 normal)
    {
        ushort size = 4 + 24;
        ushort type =
            (ushort)PacketType.PKT_CS_SENTINEL_E_PLACE;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        float[] values =
        {
            position.x,
            position.y,
            position.z,
            normal.x,
            normal.y,
            normal.z
        };

        foreach (float value in values)
        {
            Buffer.BlockCopy(
                BitConverter.GetBytes(value),
                0,
                buffer,
                offset,
                4
            );
            offset += 4;
        }

        client.Send(buffer);

        Debug.Log(
            $"[SEND SENTINEL E PLACE] " +
            $"position={position}, normal={normal}"
        );
    }

    public void SendSentinelEActivate(uint trapId)
    {
        SendSentinelETrapIdPacket(
            PacketType.PKT_CS_SENTINEL_E_ACTIVATE,
            trapId,
            "ACTIVATE"
        );
    }

    public void SendSentinelEHit(uint trapId)
    {
        SendSentinelETrapIdPacket(
            PacketType.PKT_CS_SENTINEL_E_HIT,
            trapId,
            "HIT"
        );
    }

    public void SendSentinelERecall(uint trapId)
    {
        SendSentinelETrapIdPacket(
            PacketType.PKT_CS_SENTINEL_E_RECALL,
            trapId,
            "RECALL"
        );
    }

    private void SendSentinelETrapIdPacket(
        PacketType packetType,
        uint trapId,
        string action)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)packetType;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(trapId),
            0,
            buffer,
            offset,
            4
        );

        client.Send(buffer);

        Debug.Log(
            $"[SEND SENTINEL E {action}] trapId={trapId}"
        );
    }

    private void HandleSentinelESpawn(byte[] body)
    {
        const int requiredSize = 41;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        Vector3 normal = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float hp = BitConverter.ToSingle(body, offset);
        offset += 4;

        float radius = BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnESpawn(
                trapId,
                ownerPlayerId,
                ownerTeam,
                position,
                normal,
                hp,
                radius
            );
        }
    }

    private void HandleSentinelEActivate(byte[] body)
    {
        const int requiredSize = 20;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float radius = BitConverter.ToSingle(body, offset);
        offset += 4;

        float slowDuration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float slowMultiplier =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float stunDuration =
            BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnEActivate(
                trapId,
                radius,
                slowDuration,
                slowMultiplier,
                stunDuration
            );
        }
    }

    private void HandleSentinelEDestroy(byte[] body)
    {
        const int requiredSize = 9;

        if (body == null || body.Length < requiredSize)
            return;

        uint trapId = BitConverter.ToUInt32(body, 0);
        int reason = body[4];
        float cooldownSeconds =
            BitConverter.ToSingle(body, 5);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnEDestroy(
                trapId,
                reason,
                cooldownSeconds
            );
        }
    }

    public void SendSentinelXCast(Vector3 position)
    {
        ushort size = 4 + 12;
        ushort type =
            (ushort)PacketType.PKT_CS_SENTINEL_X_CAST;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        float[] values =
        {
            position.x,
            position.y,
            position.z
        };

        foreach (float value in values)
        {
            Buffer.BlockCopy(
                BitConverter.GetBytes(value),
                0,
                buffer,
                offset,
                4
            );
            offset += 4;
        }

        client.Send(buffer);

        Debug.Log(
            $"[SEND SENTINEL X CAST] position={position}"
        );
    }

    private void HandleSentinelXSpawn(byte[] body)
    {
        const int requiredSize = 37;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint ultimateId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float radius = BitConverter.ToSingle(body, offset);
        offset += 4;

        float duration = BitConverter.ToSingle(body, offset);
        offset += 4;

        float slowMultiplier =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float protectionDuration =
            BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnXSpawn(
                ultimateId,
                ownerPlayerId,
                ownerTeam,
                position,
                radius,
                duration,
                slowMultiplier,
                protectionDuration
            );
        }
    }

    private void HandleSentinelXDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
            return;

        uint ultimateId = BitConverter.ToUInt32(body, 0);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnXDestroy(
                ultimateId
            );
        }
    }

    // 호감도 레벨 송수신

    public void SendFavorLevelRequest()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_FAVOR_LEVEL_REQUEST;

        byte[] buffer = new byte[size];
        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);
        Debug.Log("[SEND] FavorLevelRequest");
    }

    private void HandleFavorLevelResponse(byte[] body)
    {
        if (body == null || body.Length < 1) return;

        int offset = 0;
        int count = body[offset++];

        var favorDict = new Dictionary<int, int>();

        for (int i = 0; i < count; i++)
        {
            if (offset + 8 > body.Length) break;

            int characterId = BitConverter.ToInt32(body, offset); offset += 4;
            int favorLevel = BitConverter.ToInt32(body, offset); offset += 4;

            favorDict[characterId] = favorLevel;

            Debug.Log($"[RECV] FavorLevel characterId={characterId}, level={favorLevel}");
        }

        // DialogueManager에 전달
        DialogueManager.Instance?.OnRecvFavorLevels(favorDict);
    }
}
