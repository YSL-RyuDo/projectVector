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

    public void Connect(string ip, int port)
    {
        client = new TcpClient(ip, port);
        stream = client.GetStream();

        recvThread = new Thread(ReceiveLoop);
        recvThread.IsBackground = true;
        recvThread.Start();

        Debug.Log("¼­¹ö ¿¬°áµÊ");
    }

    public void Send(byte[] buffer)
    {
        stream.Write(buffer, 0, buffer.Length);
    }

    void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            int bytes = stream.Read(buffer, 0, buffer.Length);

            if (bytes <= 0)
                break;

            ushort size = BitConverter.ToUInt16(buffer, 0);
            ushort type = BitConverter.ToUInt16(buffer, 2);

            byte[] body = new byte[size - 4];
            Array.Copy(buffer, 4, body, 0, body.Length);

            OnPacket?.Invoke(type, body);
        }
    }
}