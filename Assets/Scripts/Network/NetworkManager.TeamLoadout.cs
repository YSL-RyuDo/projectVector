using System;
using System.Collections.Generic;

public partial class NetworkManager
{
    void HandleTeamLoadout(byte[] body)
    {
        if (!ApplyTeamLoadout(body,latestScoreboardData)) return;
        var me=latestScoreboardData.members.Find(m=>m.playerId==MyPlayerId && m.hasLoadout);
        if(me!=null)SkillInventoryState.ApplyUltimateState(me.ultimatePoints,me.ultimateRequired,me.ultimatePointPrice,me.ultimateUsed);
        InGameScoreboardUI.Instance?.OnRecvScoreboardUpdate(latestScoreboardData);
        StorePanelUI.Instance?.RefreshPlayerInfo();
    }
    public static bool ApplyTeamLoadout(byte[] body,ScoreboardData board)
    {
        if(body==null || board==null || body.Length<9 || BitConverter.ToUInt32(body,0)!=board.roomId || body[4]>1 || body[4]!=board.myTeam ||
            body[5]>3 || body.Length!=9+21*body[5] || body[6]<1 || body[6]>30 || BitConverter.ToUInt16(body,7)>10000)return false;
        var seen=new HashSet<uint>();
        for(int i=0;i<body[5];i++)
        {
            int at=9+i*21;uint id=BitConverter.ToUInt32(body,at);
            var member=board.members.Find(m=>m.playerId==id && m.team==board.myTeam);
            int p=BitConverter.ToInt32(body,at+4),s=BitConverter.ToInt32(body,at+8),k=BitConverter.ToInt32(body,at+12);
            if(member==null || !seen.Add(id) || !(p==-1 || p>=3 && p<=12) || !(s>=-1 && s<=2) || !(k==-1 || k==13) ||
                body[at+16]>body[at+17] || body[at+17]>100 || body[at+18]>body[6] || body[at+19]>1 || body[at+20]>1)return false;
        }
        foreach(var m in board.members)m.hasLoadout=false;
        for(int i=0;i<body[5];i++)
        {
            int at=9+i*21;var m=board.members.Find(x=>x.playerId==BitConverter.ToUInt32(body,at));
            m.hasLoadout=true;m.primaryWeaponId=BitConverter.ToInt32(body,at+4);m.pistolWeaponId=BitConverter.ToInt32(body,at+8);m.knifeWeaponId=BitConverter.ToInt32(body,at+12);
            m.armor=body[at+16];m.maxArmor=body[at+17];m.ultimatePoints=body[at+18];m.ultimateUsed=body[at+19]!=0;m.equipmentAlive=body[at+20]!=0;
            m.ultimateRequired=body[6];m.ultimatePointPrice=BitConverter.ToUInt16(body,7);
        }
        return true;
    }
}
