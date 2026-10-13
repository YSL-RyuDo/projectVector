using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class VoiceServerContext
{
    public byte Status, Scope;
    public string Server, Domain, Issuer, Account, Channel, Nickname, Token;
}

public partial class NetworkManager
{
    private uint voiceRequestSerial;
    private readonly Dictionary<uint, TaskCompletionSource<VoiceServerContext>> voiceRequests = new();

    // Independent, bounded requests: missing/older voice servers never stall game login/matchmaking.
    public async Task<VoiceServerContext> RequestVoiceAsync(byte action, string channel = "")
    {
        if (!IsConnected || !IsAuthenticated || voiceRequests.Count >= 4)
            throw new InvalidOperationException("Voice transport unavailable");
        uint id = ++voiceRequestSerial;
        var completion = new TaskCompletionSource<VoiceServerContext>();
        voiceRequests.Add(id, completion);
        try
        {
            byte[] packet = new byte[73];
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)packet.Length), 0, packet, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_VOICE_REQUEST), 0, packet, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(id), 0, packet, 4, 4);
            packet[8] = action;
            Packet.WriteFixedString(packet, 9, channel ?? "", 64);
            client.Send(packet);
            if (await Task.WhenAny(completion.Task, Task.Delay(5000)) != completion.Task)
                throw new TimeoutException("Voice server did not respond");
            return await completion.Task;
        }
        finally { voiceRequests.Remove(id); }
    }

    private void HandleVoiceResponse(byte[] body)
    {
        if (body == null || body.Length != 2726) return;
        uint id = BitConverter.ToUInt32(body, 0);
        if (!voiceRequests.TryGetValue(id, out var completion)) return;
        completion.TrySetResult(new VoiceServerContext
        {
            Status = body[4], Scope = body[5],
            Server = Packet.ReadFixedString(body, 6, 256),
            Domain = Packet.ReadFixedString(body, 262, 128),
            Issuer = Packet.ReadFixedString(body, 390, 128),
            Account = Packet.ReadFixedString(body, 518, 64),
            Channel = Packet.ReadFixedString(body, 582, 64),
            Nickname = Packet.ReadFixedString(body, 646, 32),
            Token = Packet.ReadFixedString(body, 678, 2048)
        });
    }
}
