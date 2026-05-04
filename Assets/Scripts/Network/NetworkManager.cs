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
}