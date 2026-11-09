using System;
using System.Collections.Generic;

public partial class NetworkManager
{
    public TeamPurchaseState Purchases { get; } = new TeamPurchaseState();
    public string TeamPurchaseNotice => Purchases.ResultCode switch {0=>"",1=>"지금은 구매할 수 없습니다",2=>"이미 처리되었거나 취소된 요청입니다",3=>"크레딧이 부족합니다",4=>"이미 보유한 무기입니다",_=>"요청할 수 없는 장비입니다"};
    public ScoreboardData ShopScoreboard => latestScoreboardData;
    public IReadOnlyDictionary<uint, TeamPurchaseRequest> TeamPurchaseRequests =>
        latestScoreboardData == null ? null : Purchases.ForRoom(latestScoreboardData.roomId);

    public void SendTeamPurchase(byte action, int weaponId = 0, ulong requestId = 0)
    {
        if (client == null || ReconnectBusy) return;
        var bytes = new byte[17];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)17), 0, bytes, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_TEAM_PURCHASE), 0, bytes, 2, 2);
        bytes[4] = action;
        Buffer.BlockCopy(BitConverter.GetBytes(weaponId), 0, bytes, 5, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(requestId), 0, bytes, 9, 8);
        if (action == 3) Purchases.Reset();
        client.Send(bytes);
    }

    public static bool TryReadTeamPurchases(byte[] body, ScoreboardData board, out List<TeamPurchaseRequest> entries)
    {
        entries = new List<TeamPurchaseRequest>();
        if (board == null) return false;
        var teammates = new HashSet<uint>();
        foreach (var member in board.members)
            if (member.team == board.myTeam) teammates.Add(member.playerId);
        return TeamPurchaseState.TryRead(body, board.roomId, board.myTeam, teammates, out entries);
    }

    private void HandleTeamPurchases(byte[] body)
    {
        if (TryReadTeamPurchases(body, latestScoreboardData, out var entries))
            Purchases.Replace(latestScoreboardData.roomId, entries);
    }

    private void HandleTeamPurchaseResult(byte[] body) => Purchases.ApplyResult(body);
}
