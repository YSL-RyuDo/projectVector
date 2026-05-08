using UnityEngine;
using System.Collections.Generic;
using System;

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

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    NetworkClient client = new NetworkClient();
    Queue<(ushort, byte[])> packetQueue = new();

    LoginUI loginUI;
    LobbyManager lobbyManager;

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
            string msg = result switch
            {
                (byte)LoginResult.NO_ID => "아이디 없음",
                (byte)LoginResult.WRONG_PASSWORD => "비밀번호 틀림",
                (byte)LoginResult.INVALID_INPUT => "입력 오류",
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
}