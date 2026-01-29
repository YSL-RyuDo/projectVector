using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    NetworkClient client = new NetworkClient();

    Queue<(ushort, byte[])> packetQueue = new();
    Dictionary<uint, GameObject> players = new();

    [SerializeField] GameObject localPlayerPrefab;
    [SerializeField] GameObject remotePlayerPrefab;
    [SerializeField] HPTextUI hpUI;
    uint localPlayerId;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        client.Connect("127.0.0.1", 9000);
    }

    void Update()
    {
        while (packetQueue.Count > 0)
        {
            var (type, body) = packetQueue.Dequeue();
            HandlePacket(type, body);
        }
    }

    public void EnqueuePacket(ushort type, byte[] body)
    {
        lock (packetQueue)
        {
            packetQueue.Enqueue((type, body));
        }
    }

    public void SendMoveState(float x, float y, float z, float yaw)
    {
        byte[] buffer = new byte[20];

        BitConverter.GetBytes((ushort)20).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_MOVE_STATE).CopyTo(buffer, 2);
        BitConverter.GetBytes(x).CopyTo(buffer, 4);
        BitConverter.GetBytes(y).CopyTo(buffer, 8);
        BitConverter.GetBytes(z).CopyTo(buffer, 12);
        BitConverter.GetBytes(yaw).CopyTo(buffer, 16);

        client.Send(buffer);
    }

    void HandlePacket(ushort type, byte[] body)
    {
        switch ((PacketType)type)
        {
            case PacketType.PKT_SC_HELLO:
                OnHello(body);
                break;

            case PacketType.PKT_SC_SPAWN_PLAYER:
                OnSpawn(body);
                break;

            case PacketType.PKT_SC_MOVE_STATE:
                OnMoveState(body);
                break;

            case PacketType.PKT_SC_HP_UPDATE:
                OnHPUpdate(body);
                break;

            case PacketType.PKT_SC_DIE:
                OnDie(body);
                break;
        }
    }

    void OnHello(byte[] body)
    {
        localPlayerId = BitConverter.ToUInt32(body, 0);
        Debug.Log($"[HELLO] localPlayerId={localPlayerId}");
    }

    void OnSpawn(byte[] body)
    {
        uint id = BitConverter.ToUInt32(body, 0);

        Vector3 pos = new Vector3(
            BitConverter.ToSingle(body, 4),
            BitConverter.ToSingle(body, 8),
            BitConverter.ToSingle(body, 12)
        );

        int hp = BitConverter.ToInt32(body, 16);

        bool isLocal = id == localPlayerId;

        if (!players.TryGetValue(id, out var player))
        {
            GameObject prefab =
                isLocal ? localPlayerPrefab : remotePlayerPrefab;

            player = Instantiate(prefab, pos, Quaternion.identity);
            players[id] = player;
        }

        player.transform.position = pos;
        player.SetActive(true);

        var identity = player.GetComponent<PlayerIdentity>();
        if (identity != null)
            identity.PlayerId = id;

        var hpComp = player.GetComponent<PlayerHP>();
        if (hpComp != null)
            hpComp.SetHP(hp);

        if (isLocal)
        {
            var controller = player.GetComponent<LocalPlayerController>();
            if (controller != null)
                controller.enabled = true;

            var weaponMgr = player.GetComponent<PlayerWeaponManager>();
            if (weaponMgr != null)
            {
                weaponMgr.SpawnStartWeapon();
                Debug.Log("[SPAWN] 무기 리셋 완료");
            }

            if (hpComp != null)
            {
                hpUI.Bind(hpComp);
                Debug.Log($"[SPAWN] Local Player Respawn HP={hp}");
            }
        }
        else
        {
            // 상대 플레이어: 그냥 다시 보이기만 하면 됨
            Debug.Log($"[SPAWN] Remote Player Respawn id={id} HP={hp}");
        }
    }

    void OnMoveState(byte[] body)
    {
        uint id = BitConverter.ToUInt32(body, 0);
        if (id == localPlayerId) return;

        if (!players.TryGetValue(id, out var player))
            return;

        Vector3 pos = new Vector3(
            BitConverter.ToSingle(body, 4),
            BitConverter.ToSingle(body, 8),
            BitConverter.ToSingle(body, 12)
        );
        float yaw = BitConverter.ToSingle(body, 16);

        player.GetComponent<RemotePlayerMovement>()
              ?.ApplyNetworkState(pos, yaw);
    }

    void OnHPUpdate(byte[] body)
    {
        uint id = BitConverter.ToUInt32(body, 0);
        int hp = BitConverter.ToInt32(body, 4);

        if (!players.TryGetValue(id, out var player))
            return;

        player.GetComponent<PlayerHP>()?.SetHP(hp);
    }

    void OnDie(byte[] body)
    {
        uint id = BitConverter.ToUInt32(body, 0);

        if (!players.TryGetValue(id, out var player))
            return;

        if (id == localPlayerId)
        {
            var controller = player.GetComponent<LocalPlayerController>();
            if (controller != null)
                controller.enabled = false;
        }

        player.SetActive(false);
    }



    public void SendHit(uint targetId, int damage)
    {
        byte[] buffer = new byte[12];

        // 패킷 크기
        BitConverter.GetBytes((ushort)12).CopyTo(buffer, 0);
        // 패킷 타입
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_HIT).CopyTo(buffer, 2);
        // 대상 플레이어 ID
        BitConverter.GetBytes(targetId).CopyTo(buffer, 4);
        // 데미지
        BitConverter.GetBytes(damage).CopyTo(buffer, 8);

        client.Send(buffer);
    }

}
