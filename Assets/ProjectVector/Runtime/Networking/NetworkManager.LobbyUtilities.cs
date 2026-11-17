using System;
using System.Text;

public struct LobbyUtilityReply { public byte operation, status; public ulong request; public int coin0,coin1;public string text; }
public partial class NetworkManager
{
    public event Action<LobbyUtilityReply> LobbyUtilityReceived;
    public bool SendLobbyUtility(byte operation,ulong request,string title="",string text="")
    {
        if(!IsConnected||!IsAuthenticated||ReconnectBusy||client==null)return false;
        byte[] a=Encoding.UTF8.GetBytes(title??""),b=Encoding.UTF8.GetBytes(text??"");
        if(operation>2||a.Length>240||b.Length>4500)return false;
        byte[] packet=new byte[17+a.Length+b.Length];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)packet.Length),0,packet,0,2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)430),0,packet,2,2);packet[4]=operation;
        Buffer.BlockCopy(BitConverter.GetBytes(request),0,packet,5,8);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)a.Length),0,packet,13,2);Buffer.BlockCopy(BitConverter.GetBytes((ushort)b.Length),0,packet,15,2);
        Buffer.BlockCopy(a,0,packet,17,a.Length);Buffer.BlockCopy(b,0,packet,17+a.Length,b.Length);
        client.Send(packet);return true;
    }
    void HandleLobbyUtility(byte[] body)
    {
        if(body==null||body.Length<20||body[0]>2||body.Length!=20+BitConverter.ToUInt16(body,18))return;
        try {
            var r=new LobbyUtilityReply {operation=body[0], request=BitConverter.ToUInt64(body,1),status=body[9],coin0=BitConverter.ToInt32(body,10),coin1=BitConverter.ToInt32(body,14),text=new UTF8Encoding(false,true).GetString(body,20,body.Length-20)};
            if((r.status==0||r.status==3)&&r.operation==1&&r.coin0>=0&&r.coin1>=0)MoneyManager.Instance?.SetServerBalances(r.coin0,r.coin1);
            LobbyUtilityReceived?.Invoke(r);
        } catch(DecoderFallbackException) { }
    }
}
