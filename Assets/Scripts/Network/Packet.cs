// Packet.cs
using System;
using System.Text;

public static class Packet
{
    public static byte[] MakeChat(string message)
    {
        byte[] msgBytes = Encoding.UTF8.GetBytes(message);

        ushort size = (ushort)(4 + msgBytes.Length);
        byte[] buffer = new byte[size];

        // size
        BitConverter.GetBytes(size).CopyTo(buffer, 0);
        // type
        //BitConverter.GetBytes((ushort)PacketType.CS_CHAT).CopyTo(buffer, 2);
        // payload
        msgBytes.CopyTo(buffer, 4);

        return buffer;
    }

    public static byte[] MakeRotate(float yaw)
    {
        byte[] buffer = new byte[8];

        BitConverter.GetBytes((ushort)8).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROTATE).CopyTo(buffer, 2);
        BitConverter.GetBytes(yaw).CopyTo(buffer, 4);

        return buffer;
    }

}
