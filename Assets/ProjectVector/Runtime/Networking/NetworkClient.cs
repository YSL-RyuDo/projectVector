using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetworkClient
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread recvThread;

    public Action<ushort, byte[]> OnPacket;

    // PacketHeader.size is ushort, so one packet can be up to 65,535 bytes.
    // Leave room for a maximum packet plus following TCP data.
    private const int ReceiveBufferCapacity = 128 * 1024;
    private readonly byte[] recvBuffer = new byte[ReceiveBufferCapacity];
    private int recvSize = 0;

    private volatile bool isRunning = false;
    private readonly object sendLock = new object();

    public bool IsConnected => isRunning && client != null && client.Connected;
    public string LastHost { get; private set; }
    public int LastPort { get; private set; }

    public void Connect(string ip, int port)
    {
        Close();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Environment.GetEnvironmentVariable("VECTOR_CAPTURE_OPT_IN") == "local-ranked-qa" &&
            int.TryParse(Environment.GetEnvironmentVariable("VECTOR_QA_PORT"), out int qaPort) && qaPort >= 1024 && qaPort <= 65535)
            port = qaPort;
#endif
        LastHost = ip; LastPort = port;

        try
        {
            client = new TcpClient();
            var connect = client.ConnectAsync(ip, port);
            if (!connect.Wait(5000)) throw new TimeoutException("Connection timed out");

            stream = client.GetStream();

            isRunning = true;

            recvThread = new Thread(ReceiveLoop);
            recvThread.IsBackground = true;
            recvThread.Start();

            Debug.Log("서버 연결됨");
        }
        catch (Exception e)
        {
            Debug.LogError($"서버 연결 실패: {e}");
            Close();
        }
    }

    public void Send(byte[] buffer)
    {
        if (buffer == null)
            return;

        lock (sendLock)
        {
            if (!isRunning)
                return;

            if (client == null || stream == null || !client.Connected)
                return;

            try
            {
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Send failed: {e.Message}");
                Close();
            }
        }
    }

    private void ReceiveLoop()
    {
        byte[] tempBuffer = new byte[8192];

        try
        {
            while (isRunning)
            {
                if (stream == null)
                    break;

                int bytes = stream.Read(tempBuffer, 0, tempBuffer.Length);

                if (bytes <= 0)
                    break;

                if (recvSize + bytes > recvBuffer.Length)
                {
                    Debug.LogError("[NETWORK] Receive buffer overflow (capacity 128 KB). Disconnect.");
                    break;
                }

                Buffer.BlockCopy(tempBuffer, 0, recvBuffer, recvSize, bytes);
                recvSize += bytes;

                ProcessPackets();
            }
        }
        catch (ObjectDisposedException)
        {
            // Close() 중에 stream이 닫히면 정상적으로 들어올 수 있음
        }
        catch (SocketException e)
        {
            if (isRunning)
                Debug.LogWarning($"ReceiveLoop SocketException: {e.Message}");
        }
        catch (Exception e)
        {
            if (isRunning)
                Debug.LogError($"ReceiveLoop Error: {e}");
        }

        isRunning = false;

        Debug.LogWarning("서버 연결 종료");
    }

    private void ProcessPackets()
    {
        int offset = 0;

        while (true)
        {
            if (recvSize - offset < 4)
                break;

            ushort size = BitConverter.ToUInt16(recvBuffer, offset);
            ushort type = BitConverter.ToUInt16(recvBuffer, offset + 2);

            if (size < 4)
            {
                Debug.LogError($"[NETWORK] Invalid packet size={size}");
                Close();
                return;
            }

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

    public void Close()
    {
        isRunning = false;

        try
        {
            stream?.Close();
        }
        catch { }

        try
        {
            client?.Close();
        }
        catch { }

        stream = null;
        client = null;

        recvSize = 0;

        if (recvThread != null && recvThread.IsAlive)
        {
            try
            {
                if (Thread.CurrentThread != recvThread)
                    recvThread.Join(300);
            }
            catch { }
        }

        recvThread = null;
    }
}
