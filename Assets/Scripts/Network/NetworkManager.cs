using UnityEngine;
using System.Collections.Generic;
using System;

public class LobbyCharacterData
{
    public int characterId;
    public bool owned;
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

            default:
                Debug.LogWarning($"처리 안 된 패킷 type={type}");
                break;
        }
    }

    void HandleRegister(byte[] body)
    {
        byte result = body[0];

        string msg = result switch
        {
            (byte)RegisterResult.SUCCESS => "Registration successful",
            (byte)RegisterResult.ID_DUPLICATE => "Account already exists",
            (byte)RegisterResult.NICK_DUPLICATE => "Nickname already exists",
            (byte)RegisterResult.PASSWORD_MISMATCH => "Passwords do not match",
            (byte)RegisterResult.INVALID_INPUT => "Invalid input",
            _ => "Registration failed"
        };

        loginUI?.SetResult(msg);
    }

    void HandleLogin(byte[] body)
    {
        byte result = body[0];

        string msg = result switch
        {
            (byte)LoginResult.SUCCESS => "Login successful",
            (byte)LoginResult.NO_ID => "Account not found",
            (byte)LoginResult.WRONG_PASSWORD => "Incorrect password",
            (byte)LoginResult.INVALID_INPUT => "Invalid input",
            _ => "Login failed"
        };

        loginUI?.SetResult(msg);

        if (result == (byte)LoginResult.SUCCESS)
        {
            loginUI?.MoveToLobby();
        }
    }

    void HandleLobbyEnter(byte[] body)
    {
        string nickname = Packet.ReadFixedString(body, 0, 20);
        int level = BitConverter.ToInt32(body, 20);
        int playerIconId = BitConverter.ToInt32(body, 24);
        int lobbyCharacterModelId = BitConverter.ToInt32(body, 28);
        int lobbyBackgroundId = BitConverter.ToInt32(body, 32);

        lobbyManager?.OnRecvLobbyEnter(
            nickname,
            level,
            playerIconId,
            lobbyCharacterModelId,
            lobbyBackgroundId
        );
    }

    void HandleLobbyChat(byte[] body)
    {
        uint playerId = BitConverter.ToUInt32(body, 0);
        string nickname = Packet.ReadFixedString(body, 4, 20);
        string message = Packet.ReadFixedString(body, 24, 128);

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
}