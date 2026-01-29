using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetworkClient
{
    TcpClient client;
    NetworkStream stream;
    Thread recvThread;

    public bool IsConnected => client != null && client.Connected;

    public void Connect(string ip, int port)
    {
        client = new TcpClient();
        client.Connect(ip, port);
        stream = client.GetStream();

        recvThread = new Thread(ReceiveLoop);
        recvThread.IsBackground = true;
        recvThread.Start();

        Debug.Log("[Network] Connected");
    }

    public void Send(byte[] data)
    {
        if (!IsConnected) return;
        stream.Write(data, 0, data.Length);
    }

    void ReceiveLoop()
    {
        try
        {
            while (true)
            {
                byte[] header = new byte[4];
                int read = stream.Read(header, 0, 4);
                if (read <= 0) break;

                ushort size = BitConverter.ToUInt16(header, 0);
                ushort type = BitConverter.ToUInt16(header, 2);

                int bodySize = size - 4;
                byte[] body = new byte[bodySize];

                int received = 0;
                while (received < bodySize)
                {
                    int r = stream.Read(body, received, bodySize - received);
                    if (r <= 0) break;
                    received += r;
                }

                if (NetworkManager.Instance != null)
                    NetworkManager.Instance.EnqueuePacket(type, body);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Network] Disconnected: {e.Message}");
        }
    }
}
