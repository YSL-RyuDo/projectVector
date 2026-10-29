using System;
using System.Collections.Generic;

public sealed class TeamPurchaseRequest
{
    public uint playerId,payerId;
    public ulong requestId;
    public int weaponId,price;
    public byte status;
}
public partial class NetworkManager
{
    readonly Dictionary<uint,TeamPurchaseRequest> teamPurchaseRequests=new();
    uint purchaseRoom;
    public string TeamPurchaseNotice {get;private set;}="";
    public ScoreboardData ShopScoreboard => latestScoreboardData;
    public IReadOnlyDictionary<uint,TeamPurchaseRequest> TeamPurchaseRequests =>
        latestScoreboardData!=null && purchaseRoom==latestScoreboardData.roomId ? teamPurchaseRequests : null;

    public void SendTeamPurchase(byte action,int weaponId=0,ulong requestId=0)
    {
        if(client==null || ReconnectBusy)return;
        var bytes=new byte[17];Buffer.BlockCopy(BitConverter.GetBytes((ushort)17),0,bytes,0,2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_TEAM_PURCHASE),0,bytes,2,2);
        bytes[4]=action;Buffer.BlockCopy(BitConverter.GetBytes(weaponId),0,bytes,5,4);Buffer.BlockCopy(BitConverter.GetBytes(requestId),0,bytes,9,8);
        if(action==3){teamPurchaseRequests.Clear();TeamPurchaseNotice="";}
        client.Send(bytes);
    }
    public static bool TryReadTeamPurchases(byte[] body,ScoreboardData board,out List<TeamPurchaseRequest> entries)
    {
        entries=new();
        if(body==null || board==null || body.Length<7 || BitConverter.ToUInt32(body,0)!=board.roomId || body[4]!=board.myTeam || body[4]>1 || body[6]>3 || body.Length!=7+25*body[6])return false;
        var seen=new HashSet<uint>();var tokens=new HashSet<ulong>();
        for(int i=0;i<body[6];i++) {
            int at=7+25*i;var q=new TeamPurchaseRequest{playerId=BitConverter.ToUInt32(body,at),requestId=BitConverter.ToUInt64(body,at+4),weaponId=BitConverter.ToInt32(body,at+12),price=BitConverter.ToInt32(body,at+16),payerId=BitConverter.ToUInt32(body,at+20),status=body[at+24]};
            if(!board.members.Exists(m=>m.playerId==q.playerId && m.team==board.myTeam) || !seen.Add(q.playerId) || q.requestId==0 || !tokens.Add(q.requestId) || q.weaponId<1 || q.weaponId>12 || q.price<1 || q.price>10000 || q.status<1 || q.status>2 ||
                (q.status==1 && q.payerId!=0) || (q.status==2 && !board.members.Exists(m=>m.playerId==q.payerId && m.team==board.myTeam)))return false;
            entries.Add(q);
        }
        return true;
    }
    void HandleTeamPurchases(byte[] body)
    {
        if(!TryReadTeamPurchases(body,latestScoreboardData,out var entries))return;
        purchaseRoom=latestScoreboardData.roomId;teamPurchaseRequests.Clear();
        foreach(var q in entries)teamPurchaseRequests.Add(q.playerId,q);
    }
    void HandleTeamPurchaseResult(byte[] body)
    {
        if(body==null || body.Length!=5 || body[0]>5)return;
        TeamPurchaseNotice=body[0] switch {0=>"",1=>"지금은 구매할 수 없습니다",2=>"이미 처리되었거나 취소된 요청입니다",3=>"크레딧이 부족합니다",4=>"이미 보유한 무기입니다",_=>"요청할 수 없는 장비입니다"};
        StorePanelUI.Instance?.SetCreditDirect(BitConverter.ToInt32(body,1));
    }
}
