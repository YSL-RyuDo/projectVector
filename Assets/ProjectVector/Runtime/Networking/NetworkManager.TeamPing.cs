using System;
using System.IO;
using Game.Pings;
using UnityEngine;

public partial class NetworkManager
{
    public event Action<ConfirmedPing> TeamPingReceived;

    public void SendTeamPing(PingRequest request)
    {
        if (!IsConnected || !IsAuthenticated || ReconnectBusy || !Guid.TryParse(request.RequestId,out var id)) return;
        using var stream=new MemoryStream(33);
        using var writer=new BinaryWriter(stream);
        writer.Write((ushort)33);writer.Write((ushort)PacketType.PKT_CS_TEAM_PING);
        writer.Write(id.ToByteArray());writer.Write((byte)request.Type);
        writer.Write(request.Position.x);writer.Write(request.Position.y);writer.Write(request.Position.z);
        client.Send(stream.ToArray());
    }

    void HandleTeamPing(byte[] body)
    {
        if (body==null || body.Length!=28 || ReconnectBusy) return;
        ulong id=BitConverter.ToUInt64(body,0);uint sender=BitConverter.ToUInt32(body,8);
        int team=body[12];var kind=(PingType)body[13];int ms=BitConverter.ToUInt16(body,14);
        var position=new Vector3(BitConverter.ToSingle(body,16),BitConverter.ToSingle(body,20),BitConverter.ToSingle(body,24));
        if(id==0 || sender==0 || team>1 || (byte)kind>3 || ms==0 || ms>3000 || !PingValidation.IsFinite(position))return;
        TeamPingReceived?.Invoke(new ConfirmedPing(id.ToString(),sender,team,kind,position,ms*.001f));
    }
}
