using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetworkClient
{
    TcpClient client;
    NetworkStream stream;
    Thread recvThread;

    public Action<ushort, byte[]> OnPacket;

    private byte[] recvBuffer = new byte[8192];
    private int recvSize = 0;

    public bool IsConnected => client != null && client.Connected;

    public void Connect(string ip, int port)
    {
        client = new TcpClient(ip, port);
        stream = client.GetStream();

        recvThread = new Thread(ReceiveLoop);
        recvThread.IsBackground = true;
        recvThread.Start();

        Debug.Log("서버 연결됨");
    }

    public void Send(byte[] buffer)
    {
        if (stream == null || !client.Connected)
            return;

        stream.Write(buffer, 0, buffer.Length);
    }

    void ReceiveLoop()
    {
        byte[] tempBuffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytes = stream.Read(tempBuffer, 0, tempBuffer.Length);

                if (bytes <= 0)
                    break;

                Buffer.BlockCopy(tempBuffer, 0, recvBuffer, recvSize, bytes);
                recvSize += bytes;

                ProcessPackets();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ReceiveLoop Error: {e}");
        }

        Debug.LogWarning("서버 연결 종료");
    }

    void ProcessPackets()
    {
        int offset = 0;

        while (true)
        {
            if (recvSize - offset < 4)
                break;

            ushort size = BitConverter.ToUInt16(recvBuffer, offset);
            ushort type = BitConverter.ToUInt16(recvBuffer, offset + 2);

            if (recvSize - offset < size)
                break;

            byte[] body = new byte[size - 4];
            Buffer.BlockCopy(recvBuffer, offset + 4, body, 0, body.Length);

            OnPacket?.Invoke(type, body);

            offset += size;
        }

        if (offset > 0)
        {
            Buffer.BlockCopy(recvBuffer, offset, recvBuffer, 0, recvSize - offset);
            recvSize -= offset;
        }
    }
}