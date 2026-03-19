using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    NetworkClient client = new NetworkClient();

    Queue<(ushort, byte[])> packetQueue = new();

    LoginUI loginUI;

    void Awake()
    {
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
        while (packetQueue.Count > 0)
        {
            var (type, body) = packetQueue.Dequeue();
            HandlePacket(type, body);
        }
    }

    public void SetLoginUI(LoginUI ui)
    {
        loginUI = ui;
    }

    public void SendRegister(string id, string nick, string pw, string confirm)
    {
        byte[] buffer = new byte[4 + 20 + 20 + 64 + 64];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)0).CopyTo(buffer, 2);

        WriteString(buffer, 4, id, 20);
        WriteString(buffer, 24, nick, 20);
        WriteString(buffer, 44, pw, 64);
        WriteString(buffer, 108, confirm, 64);

        client.Send(buffer);
    }

    public void SendLogin(string id, string pw)
    {
        byte[] buffer = new byte[4 + 20 + 64];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)2).CopyTo(buffer, 2);

        WriteString(buffer, 4, id, 20);
        WriteString(buffer, 24, pw, 64);

        client.Send(buffer);
    }

    void HandlePacket(ushort type, byte[] body)
    {
        switch (type)
        {
            case 1:
                HandleRegister(body);
                break;

            case 3:
                HandleLogin(body);
                break;
        }
    }
    void WriteString(byte[] buffer, int offset, string str, int size)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        int len = Mathf.Min(bytes.Length, size);
        System.Array.Copy(bytes, 0, buffer, offset, len);
    }

    void HandleRegister(byte[] body)
    {
        byte result = body[0];

        string msg = result switch
        {
            0 => "Registration successful",
            1 => "Account already exists",
            2 => "Nickname already exists",
            3 => "Passwords do not match",
            4 => "Invalid input",
            _ => "Registration failed"
        };

        loginUI?.SetResult(msg);
    }

    void HandleLogin(byte[] body)
    {
        byte result = body[0];

        string msg = result switch
        {
            0 => "Login successful",
            1 => "Account not found",
            2 => "Incorrect password",
            3 => "Invalid input",
            _ => "Login failed"
        };

        loginUI?.SetResult(msg);

        if (result == 0)
        {
            Debug.Log("Login success ¡æ move to next scene");
        }
    }
}