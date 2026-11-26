using System;
using System.Text;
using UnityEngine;

public partial class NetworkManager
{
    public VectorAccountSettingsSync AccountSettings {get;private set;}
    public static bool CanEditAccountSettings=>VectorSettingsProfile.Applying||Instance?.AccountSettings?.CanEdit!=false;
    void InitializeAccountSettings()=>AccountSettings=new VectorAccountSettingsSync(SendAccountSettings);
    bool SendAccountSettings(byte operation,ulong request,ulong revision,string payload)
    {
        if(!IsConnected||!IsAuthenticated||ReconnectBusy)return false;
        byte[] text=Encoding.UTF8.GetBytes(payload);if(text.Length>VectorSettingsProfile.MaxBytes)return false;
        byte[] packet=new byte[23+text.Length];
        BitConverter.GetBytes((ushort)packet.Length).CopyTo(packet,0);BitConverter.GetBytes((ushort)432).CopyTo(packet,2);
        packet[4]=operation;BitConverter.GetBytes(request).CopyTo(packet,5);BitConverter.GetBytes(revision).CopyTo(packet,13);BitConverter.GetBytes((ushort)text.Length).CopyTo(packet,21);text.CopyTo(packet,23);
        client.Send(packet);return true;
    }
    void HandleAccountSettings(byte[] body)
    {
        if(body==null||body.Length<20||body[0]>1||body.Length!=20+BitConverter.ToUInt16(body,18))return;
        try{AccountSettings?.Receive(body[0],BitConverter.ToUInt64(body,1),body[9],BitConverter.ToUInt64(body,10),new UTF8Encoding(false,true).GetString(body,20,body.Length-20),Time.unscaledTime);}catch(DecoderFallbackException){}
    }
}
