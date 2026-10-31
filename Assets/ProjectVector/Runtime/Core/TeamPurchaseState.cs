using System;
using System.Collections.Generic;

// Plain protocol/state types. This assembly cannot reference UnityEngine,
// scenes, UI singletons or NetworkManager.
public sealed class TeamPurchaseRequest
{
    public uint playerId, payerId;
    public ulong requestId;
    public int weaponId, price;
    public byte status;
}

public sealed class TeamPurchaseState
{
    private readonly Dictionary<uint, TeamPurchaseRequest> requests = new();
    private uint roomId;

    public byte ResultCode { get; private set; }
    public event Action<int> CreditsReceived;

    public IReadOnlyDictionary<uint, TeamPurchaseRequest> ForRoom(uint room) =>
        room == roomId ? requests : null;

    public void Reset()
    {
        requests.Clear();
        roomId = 0;
        ResultCode = 0;
    }

    public void Replace(uint room, List<TeamPurchaseRequest> entries)
    {
        roomId = room;
        requests.Clear();
        foreach (var request in entries) requests.Add(request.playerId, request);
    }

    public bool ApplyResult(byte[] body)
    {
        if (body == null || body.Length != 5 || body[0] > 5) return false;
        int credits = BitConverter.ToInt32(body, 1);
        if (credits < 0) return false;
        ResultCode = body[0];
        CreditsReceived?.Invoke(credits);
        return true;
    }

    public static bool TryRead(byte[] body, uint expectedRoom, byte expectedTeam,
        ISet<uint> teammates, out List<TeamPurchaseRequest> entries)
    {
        entries = new List<TeamPurchaseRequest>();
        if (body == null || teammates == null || body.Length < 7 ||
            BitConverter.ToUInt32(body, 0) != expectedRoom || body[4] != expectedTeam ||
            body[4] > 1 || body[6] > 3 || body.Length != 7 + 25 * body[6]) return false;

        var players = new HashSet<uint>();
        var tokens = new HashSet<ulong>();
        for (int i = 0; i < body[6]; i++)
        {
            int offset = 7 + 25 * i;
            var request = new TeamPurchaseRequest
            {
                playerId = BitConverter.ToUInt32(body, offset),
                requestId = BitConverter.ToUInt64(body, offset + 4),
                weaponId = BitConverter.ToInt32(body, offset + 12),
                price = BitConverter.ToInt32(body, offset + 16),
                payerId = BitConverter.ToUInt32(body, offset + 20),
                status = body[offset + 24]
            };
            if (!teammates.Contains(request.playerId) || !players.Add(request.playerId) ||
                request.requestId == 0 || !tokens.Add(request.requestId) ||
                request.weaponId < 1 || request.weaponId > 12 || request.price < 1 || request.price > 10000 ||
                request.status < 1 || request.status > 2 ||
                (request.status == 1 && request.payerId != 0) ||
                (request.status == 2 && !teammates.Contains(request.payerId))) return false;
            entries.Add(request);
        }
        return true;
    }
}
