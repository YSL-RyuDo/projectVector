using System;
using System.Collections.Generic;
public partial class NetworkManager
{
    // Detail event follows the unchanged death event on the same TCP stream.
    public static bool ParseKillFeed(byte[] body,out uint killer,out uint dead,out int weapon,out bool headshot,out string assists)
    {
        killer=dead=0;weapon=-2;headshot=false;assists="";
        if(body==null||body.Length<14)return false;
        int count=body[13];if(count>7||body.Length!=14+28*count||body[12]>1)return false;
        killer=BitConverter.ToUInt32(body,0);dead=BitConverter.ToUInt32(body,4);weapon=BitConverter.ToInt32(body,8);
        if(killer==0||dead==0||weapon < -1 || weapon>13)return false;
        var ids=new HashSet<uint>();var names=new List<string>();
        for(int i=0;i<count;i++)
        {
            int p=14+i*28;uint id=BitConverter.ToUInt32(body,p);
            if(id==0||id==killer||id==dead||!ids.Add(id))return false;
            names.Add(KillLogEntryUI.SafeName(Packet.ReadFixedString(body,p+8,20)));
        }
        headshot=body[12]==1;assists=string.Join(", ",names);return true;
    }
    void HandleKillFeed(byte[] body)
    {if(ParseKillFeed(body,out uint killer,out uint dead,out int weapon,out bool head,out string assists))KillLogManager.Instance?.ApplyDetails(killer,dead,weapon,head,assists);}
}
