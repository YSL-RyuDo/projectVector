using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using System.Security.Principal;

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
}

public class ScoreboardData
{
    public uint roomId;
    public byte myTeam;
    public List<ScoreboardMemberData> members = new();
}


public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    NetworkClient client = new NetworkClient();
    Queue<(ushort, byte[])> packetQueue = new();

    LoginUI loginUI;
    LobbyManager lobbyManager;
    private PickManager pickManager;
    private MatchManager matchManager;

    private uint myPlayerId;
    public uint MyPlayerId => myPlayerId;

    private string myUserId = "";
    public string MyUserId => myUserId;

    private uint pendingReturnRoomId = 0;

    public bool HasPendingReturnRoom => pendingReturnRoomId != 0;
    public uint PendingReturnRoomId => pendingReturnRoomId;

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
        myUserId = id.Trim();

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

    void HandlePacket(ushort type, byte[] body)
    {
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

            case (ushort)PacketType.PKT_SC_LOBBY_CHARACTER_SELECT_RESULT:
                HandleLobbyCharacterSelectResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_BACKGROUND_SELECT_RESULT:
                HandleLobbyBackgroundSelectResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_CHARACTER_LIST:
                HandleLobbyCharacterList(body);
                break;

            case (ushort)PacketType.PKT_SC_TEMP_GAME_ROOM_ENTER:
                HandleTempGameRoomEnter(body);
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

            case (ushort)PacketType.PKT_SC_HP_UPDATE:
                HandleHPUpdate(body);
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
        string nickname = Packet.ReadFixedString(body, 0, 20);

        int level = BitConverter.ToInt32(body, 20);
        int currentExp = BitConverter.ToInt32(body, 24);
        int requiredExp = BitConverter.ToInt32(body, 28);

        int playerIconId = BitConverter.ToInt32(body, 32);
        int lobbyCharacterModelId = BitConverter.ToInt32(body, 36);
        int lobbyBackgroundId = BitConverter.ToInt32(body, 40);

        Debug.Log($"[CLIENT LOBBY ENTER] nickname={nickname}, level={level}, exp={currentExp}/{requiredExp}, icon={playerIconId}, char={lobbyCharacterModelId}, bg={lobbyBackgroundId}");

        lobbyManager?.OnRecvLobbyEnter(
            nickname,
            level,
            currentExp,
            requiredExp,
            playerIconId,
            lobbyCharacterModelId,
            lobbyBackgroundId
        );
    }
    void HandleLobbyChat(byte[] body)
    {
        Debug.Log($"[CLIENT HANDLE LOBBY CHAT] bodyLen={body.Length}");

        uint playerId = BitConverter.ToUInt32(body, 0);
        string nickname = Packet.ReadFixedString(body, 4, 20);
        string message = Packet.ReadFixedString(body, 24, 128);

        Debug.Log($"[CLIENT CHAT PARSED] playerId={playerId} nickname={nickname} message={message}");

        lobbyManager?.OnRecvLobbyChat(playerId, nickname, message);
    }

    void HandleLobbyCharacterSelectResult(byte[] body)
    {

        byte result = body[0];
        int modelId = BitConverter.ToInt32(body, 1);

        
        lobbyManager?.OnRecvLobbyCharacterSelectResult(result, modelId);
    }

    void HandleLobbyBackgroundSelectResult(byte[] body)
    {
        byte result = body[0];
        int backgroundId = BitConverter.ToInt32(body, 1);

        lobbyManager?.OnRecvLobbyBackgroundSelectResult(result, backgroundId);
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

        lobbyManager?.OnRecvLobbyCharacterList(list, currentCharacterModelId);
    }

    public void SendTempGameRoomEnter()
    {
        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_TEMP_GAME_ROOM_ENTER).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    public void SendTempGameStart()
    {
        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_TEMP_GAME_START).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    void HandleTempGameRoomEnter(byte[] body)
    {
        byte result = body[0];
        byte playerCount = body[1];

        if (result != 0)
        {
            Debug.LogWarning("[TEMP GAME ROOM] enter fail");
            return;
        }

        List<TempGamePlayerData> players = new List<TempGamePlayerData>();

        int offset = 2;
        for (int i = 0; i < 3; i++)
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

            players.Add(data);
        }

        lobbyManager?.OnRecvTempGameRoomEnter(playerCount, players);
    }

    void HandleTempGameStartResult(byte[] body)
    {
        byte result = body[0];
        lobbyManager?.OnRecvTempGameStartResult(result);
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

        LobbyManager.Instance.OnRecvRoomList(rooms);
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

        lobbyManager?.OnRecvRoomCreateResult(roomState);
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

        lobbyManager?.OnRecvRoomLeaveResult(result, roomId, roomDeleted != 0);
    }

    void HandleRoomEnterResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        uint roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        Debug.Log($"[ROOM ENTER RESULT] result={result}, roomId={roomId}");

        lobbyManager?.OnRecvRoomEnterResult(result, roomId);
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

        lobbyManager?.OnRecvRoomState(roomState);
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

        lobbyManager?.OnRecvRoomSlotChangeResult(
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

        lobbyManager?.OnRecvRoomSlotSwapRequest(data);
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

        lobbyManager?.OnRecvRoomTransferLeaderResult(
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

        lobbyManager?.OnRecvRoomKickResult(
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

        lobbyManager?.OnRecvRoomKicked(roomId, kickerNickname);
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

        lobbyManager?.OnRecvRoomGameStartResult(result);
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

            if(i<memberCount)
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
        uint roomId = BitConverter.ToUInt32(body, 0);

        Debug.Log($"[MATCH START] roomId={roomId}");

        pickManager?.OnRecvMatchStart(roomId);
    }

    public void SendGameLoadReady()
    {
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
        Debug.Log("[RECV] GAME ALL READY");

        int offset = 0;
        GameAllReadyData data = new GameAllReadyData();

        data.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        data.prepareSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        byte memberCount = body[offset];
        offset += 1;

        for(int i=0; i<6; i++)
        {
            GameReadyMemberData member = new GameReadyMemberData();

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
                data.members.Add(member);
        }

        Debug.Log($"[RECV] GAME ALL READY roomId={data.roomId}, memberCount={data.members.Count}");

        if (matchManager != null)
        {
            matchManager.OnRecvGameAllReady(data);

            foreach (GameReadyMemberData member in data.members)
            {
                ApplyTeamToPlayer(member.playerId, member.team);
            }

            RefreshPlayerTagsByTeam();
        }
        else
        {
            Debug.LogError("[NETWORK] matchManager is null");
        }
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

    public void SendMoveState(float x, float y, float z, float yaw, float pitch)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4;
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

        client.Send(buffer);
    }

    private void HandleMoveState(byte[] body)
    {
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

        if (playerId == MyPlayerId)
            return;

        PlayerMovement movement = FindPlayerMovementById(playerId);

        if (movement == null)
        {
            Debug.LogWarning($"[MOVE RECV] PlayerMovement not found. playerId={playerId}");
            return;
        }

        movement.OnReceiveRemoteMove(new Vector3(x, y, z), yaw, pitch);
    }

    private void HandleHPUpdate(byte[] body)
    {
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

        Debug.Log(
            $"[HP UPDATE] playerId={playerId}, " +
            $"hp={hp}, barrier={barrier}, maxHp={maxHp}, maxBarrier={maxBarrier}"
        );

        ApplyHPUpdate(playerId, hp, barrier, maxHp, maxBarrier);
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

    public void SendHit(uint targetPlayerId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_HIT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(targetPlayerId), 0, buffer, offset, 4);

        client.Send(buffer);

        Debug.Log($"[SEND HIT] targetPlayerId={targetPlayerId}");
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
            if (SpectatorManager.Instance != null)
                SpectatorManager.Instance.StopSpectating();

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

            PlayerItem item = targetIdentity.GetComponent<PlayerItem>();

            if (item != null)
                item.ResetToDefault();

            if (movement != null)
            {
                movement.SetLocalPlayer(true);
                movement.CanMove = true;
            }

            if (cameraController != null)
            {
                cameraController.SetLocalPlayer(true);

                if (cameraController.PlayerCamera != null)
                {
                    cameraController.PlayerCamera.transform.localPosition = Vector3.zero;
                    cameraController.PlayerCamera.transform.localRotation = Quaternion.identity;
                }
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
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log("[NETWORK] NetworkManager destroyed. Close client.");
            client?.Close();
            Instance = null;
        }
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
        int offset = 0;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        int magAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int reserveAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        Debug.Log(
            $"[AMMO UPDATE] weaponId={weaponId}, magAmmo={magAmmo}, reserveAmmo={reserveAmmo}"
        );

        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.SetBulletText(magAmmo, reserveAmmo);
        }

        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        for (int i = 0; i < identities.Length; i++)
        {
            PlayerIdentity identity = identities[i];

            if (identity == null)
                continue;

            if (identity.PlayerId != MyPlayerId)
                continue;

            PlayerItem item = identity.GetComponent<PlayerItem>();
            if (item == null)
                break;

            Weapon weapon = item.GetCurrentWeapon();
            if (weapon != null && weapon.GetWeaponId() == weaponId)
            {
                weapon.SetAmmo(magAmmo, reserveAmmo);
            }

            break;
        }
    }

    public void SendFire()
    {
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

    private void HandleMatchResultDetail(byte[] body)
    {
        int offset = 0;

        MatchResultCache.Clear();

        MatchResultCache.winnerTeam = body[offset];
        offset += 1;

        MatchResultCache.scoreA = body[offset];
        offset += 1;

        MatchResultCache.scoreB = body[offset];
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

            MatchResultCache.members.Add(member);
        }

        Debug.Log(
            $"[MATCH RESULT DETAIL] winner={MatchResultCache.winnerTeam}, " +
            $"score={MatchResultCache.scoreA}:{MatchResultCache.scoreB}, " +
            $"members={MatchResultCache.members.Count}"
        );

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

            data.members.Add(member);
        }

        if (InGameScoreboardUI.Instance != null)
            InGameScoreboardUI.Instance.OnRecvScoreboardUpdate(data);
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

        // 핵심: 스파이크를 버린 플레이어는 더 이상 스파이크 보유자가 아님
        ApplySpikeDroppedToPlayer(dropperPlayerId);
    }

    private void HandleSpikeDefuseState(byte[] body)
    {
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

        uint deadPlayerId = BitConverter.ToUInt32(body, 0);

        Debug.Log($"[DIE] playerId={deadPlayerId}");

        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        foreach (PlayerIdentity identity in identities)
        {
            if (identity.PlayerId != deadPlayerId)
                continue;

            PlayerDeathController death =
                identity.GetComponent<PlayerDeathController>();

            if (death != null)
                death.OnDead();
            else
                Debug.LogError($"[DIE] PlayerDeathController missing. object={identity.name}");

            break;
        }

        if (deadPlayerId == MyPlayerId)
        {
            PlayerIdentity myIdentity = null;

            PlayerIdentity[] allIdentities =
                FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

            foreach (PlayerIdentity identity in allIdentities)
            {
                if (identity.PlayerId == MyPlayerId)
                {
                    myIdentity = identity;
                    break;
                }
            }

            if (myIdentity != null)
            {
                PlayerSpike playerSpike = myIdentity.GetComponent<PlayerSpike>();

                if (playerSpike == null)
                    playerSpike = myIdentity.GetComponentInChildren<PlayerSpike>(true);

                if (playerSpike != null)
                    playerSpike.ForceCancelPlant();
                else
                    Debug.LogError("[DIE] PlayerSpike missing on local player");

                PlayerItem item = myIdentity.GetComponent<PlayerItem>();
                if (item != null)
                    item.SetWeaponVisibility(false);

                FPSCameraLook cameraLook = myIdentity.GetComponentInChildren<FPSCameraLook>(true);

                if (cameraLook != null)
                    cameraLook.ForceExitADS();
                else
                    Debug.LogError("[DIE] FPSCameraLook missing on local player");
            }

            if (SpectatorManager.Instance != null)
                SpectatorManager.Instance.StartSpectating();
            else
                Debug.LogError("[DIE] SpectatorManager.Instance is null");
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

            Debug.Log($"[TEAM APPLY] playerId={playerId}, team={team}");
            return;
        }
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

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        Debug.Log($"[WEAPON SWITCH NOTIFY] playerId={playerId}, weaponId={weaponId}");

        PlayerIdentity identity = FindPlayerIdentityById(playerId);

        if (identity == null)
        {
            Debug.LogWarning($"[WEAPON SWITCH NOTIFY] player not found. playerId={playerId}");
            return;
        }

        PlayerItem item = identity.GetComponent<PlayerItem>();

        if (item == null)
            item = identity.GetComponentInChildren<PlayerItem>(true);

        if (item == null)
        {
            Debug.LogWarning($"[WEAPON SWITCH NOTIFY] PlayerItem missing. playerId={playerId}");
            return;
        }

        item.ApplyWeaponSwitchFromServer(weaponId);
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

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        bool isADS = body[offset] != 0;
        offset += 1;

        Debug.Log($"[ADS STATE] playerId={playerId}, isADS={isADS}");

        PlayerIdentity identity = FindPlayerIdentityById(playerId);

        if (identity == null)
        {
            Debug.LogWarning($"[ADS STATE] player not found. playerId={playerId}");
            return;
        }

        FPSCameraLook look = identity.GetComponentInChildren<FPSCameraLook>(true);

        if (look != null)
            look.SetADSFromServer(isADS);

        PlayerItem item = identity.GetComponent<PlayerItem>();

        if (item == null)
            item = identity.GetComponentInChildren<PlayerItem>(true);

        if (item != null)
            item.SetADSVisualFromServer(isADS);
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
}