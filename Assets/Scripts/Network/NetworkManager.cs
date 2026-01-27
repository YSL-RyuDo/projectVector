using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    NetworkClient client = new NetworkClient();

    Queue<(ushort, byte[])> packetQueue = new();
    Dictionary<uint, GameObject> players = new();

    [SerializeField] GameObject playerPrefab;

    void Awake()
    {
        Instance = this;
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

    public void SendMove(float x, float z, float yaw)
    {
        byte[] buffer = new byte[16];

        BitConverter.GetBytes((ushort)16).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_MOVE).CopyTo(buffer, 2);
        BitConverter.GetBytes(x).CopyTo(buffer, 4);
        BitConverter.GetBytes(z).CopyTo(buffer, 8);
        BitConverter.GetBytes(yaw).CopyTo(buffer, 12);

        client.Send(buffer);
    }

    public void SendRotate(float yaw)
    {
        var packet = Packet.MakeRotate(yaw);
        client.Send(packet);
    }

    void HandlePacket(ushort type, byte[] body)
    {
        switch ((PacketType)type)
        {
            case PacketType.PKT_SC_SPAWN_PLAYER:
                {
                    uint id = BitConverter.ToUInt32(body, 0);
                    float x = BitConverter.ToSingle(body, 4);
                    float y = BitConverter.ToSingle(body, 8);
                    float z = BitConverter.ToSingle(body, 12);

                    if (players.ContainsKey(id))
                        return;

                    GameObject obj = Instantiate(
                        playerPrefab,
                        new Vector3(x, y, z),
                        Quaternion.identity
                    );

                    players[id] = obj;
                    break;
                }

            case PacketType.PKT_SC_MOVE:
            {
                uint id = BitConverter.ToUInt32(body, 0);
                float x = BitConverter.ToSingle(body, 4);
                float y = BitConverter.ToSingle(body, 8);
                float z = BitConverter.ToSingle(body, 12);
                float yaw = BitConverter.ToSingle(body, 16);

                if (players.TryGetValue(id, out var obj))
                {
                    obj.transform.position = new Vector3(x, y, z);
                    obj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                }
                break;
            }
            case PacketType.PKT_SC_ROTATE:
                {
                    uint id = BitConverter.ToUInt32(body, 0);
                    float yaw = BitConverter.ToSingle(body, 4);

                    if (players.TryGetValue(id, out var obj))
                    {
                        obj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    }
                    break;
                }

        }
    }
}
