using System;
using System.Collections.Generic;

public sealed class PickActionMember
{
    public uint playerId, peerId;
    public int characterId;
    public bool locked, requester;
    public ulong exchangeId;
    public ushort remainingMs;
}
public sealed class PickActionsState
{
    public uint roomId;
    public ulong revision;
    public bool open;
    public readonly List<PickActionMember> members = new();
}
public partial class NetworkManager
{
    public void SendPickAction(uint roomId, byte action, uint targetId = 0, ulong exchangeId = 0)
    {
        if (client == null || ReconnectBusy || roomId == 0) return;
        byte[] b = new byte[21];
        BitConverter.GetBytes((ushort)b.Length).CopyTo(b, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PICK_ACTION).CopyTo(b, 2);
        BitConverter.GetBytes(roomId).CopyTo(b, 4); b[8] = action;
        BitConverter.GetBytes(exchangeId).CopyTo(b, 9);
        BitConverter.GetBytes(targetId).CopyTo(b, 17); client.Send(b);
    }
    public static bool TryReadPickActions(byte[] b, out PickActionsState state)
    {
        state = null;
        if (b == null || b.Length < 14 || b[12] > 1 || b[13] > 3 || b.Length != 14 + 24 * b[13]) return false;
        var s = new PickActionsState { roomId = BitConverter.ToUInt32(b, 0), revision = BitConverter.ToUInt64(b, 4), open = b[12] != 0 };
        var ids = new HashSet<uint>();
        for (int i = 0; i < b[13]; i++)
        {
            int p = 14 + 24 * i;
            var m = new PickActionMember { playerId = BitConverter.ToUInt32(b, p), characterId = BitConverter.ToInt32(b, p + 4), locked = b[p + 8] != 0,
                peerId = BitConverter.ToUInt32(b, p + 9), exchangeId = BitConverter.ToUInt64(b, p + 13), remainingMs = BitConverter.ToUInt16(b, p + 21), requester = b[p + 23] != 0 };
            if (m.playerId == 0 || !ids.Add(m.playerId) || b[p + 8] > 1 || b[p + 23] > 1 || m.characterId < -1 || m.characterId > 3 || m.remainingMs > 8000 || (m.locked && m.characterId < 0)) return false;
            if ((m.exchangeId == 0) != (m.peerId == 0) || m.peerId == m.playerId || (!s.open && m.exchangeId != 0)) return false;
            s.members.Add(m);
        }
        foreach (var m in s.members) if (m.exchangeId != 0)
        {
            var peer = s.members.Find(x => x.playerId == m.peerId);
            if (peer == null || peer.peerId != m.playerId || peer.exchangeId != m.exchangeId || peer.requester == m.requester || !m.locked || !peer.locked) return false;
        }
        state = s; return s.roomId != 0;
    }
    void HandlePickActions(byte[] body)
    {
        if (TryReadPickActions(body, out var state)) pickManager?.OnRecvPickActions(state);
    }
    void HandlePickActionResult(byte[] body)
    {
        if (body == null || body.Length != 5 || body[4] > 6) return;
        pickManager?.OnPickActionResult(BitConverter.ToUInt32(body, 0), body[4]);
    }
    void HandlePickShowcase(byte[] b)
    {
        if (b == null || b.Length != 25 || b[20] > 2) return;
        uint matches = BitConverter.ToUInt32(b, 12), wins = BitConverter.ToUInt32(b, 16);
        int character = BitConverter.ToInt32(b, 8);
        if (character < 0 || character > 3 || matches < 5 || wins > matches || (ulong)wins * 2 < matches) return;
        pickManager?.OnPickShowcase(BitConverter.ToUInt32(b, 0), BitConverter.ToUInt32(b, 4), character, matches, wins, b[20], BitConverter.ToUInt32(b, 21));
    }
}
