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

        loginUI?.SetStatus("서버 연결중...");

        client.OnPacket += (type, body) =>
        {
            lock (packetQueue)
            {
                packetQueue.Enqueue((type, body));
            }
        };

        client.Connect("127.0.0.1", 9000);

        loginUI?.SetStatus("서버 연결됨");
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
        loginUI?.SetLoading(false);

        byte result = body[0];

        string msg = result switch
        {
            0 => "회원가입이 완료되었습니다.",
            1 => "이미 존재하는 아이디입니다.",
            2 => "닉네임이 이미 존재합니다.",
            3 => "비밀번호가 일치하지 않습니다.",
            4 => "입력값 형식이 올바르지 않습니다.",
            _ => "회원가입 실패"
        };

        loginUI?.SetResult(msg);
        
        if (result == 0)
        {
            loginUI?.ClearInputs();
            loginUI?.SetResult("회원가입이 완료되었습니다.");

            loginUI?.SwitchToLoginMode();
        }

        
    }

    void HandleLogin(byte[] body)
    {
        byte result = body[0];

        loginUI?.SetLoading(false);

        string msg = result switch
        {
            0 => "로그인 성공",
            1 => "존재하지 않는 계정입니다.",
            2 => "비밀번호가 올바르지 않습니다.",
            3 => "입력값 형식이 올바르지 않습니다.",
            _ => "로그인 실패"
        };

        loginUI?.SetResult(msg);

        if (result == 0)
        {
            Debug.Log("Login success → move to next scene");
            loginUI?.SetResult("로그인 성공");
            loginUI?.MoveToLobby();
        }

        else
        {
            loginUI?.ClearInputs();
        }
        
    }
}