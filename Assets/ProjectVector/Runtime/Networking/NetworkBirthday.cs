using System;

public partial class NetworkManager
{
    public static byte[] BirthdayPacket(uint requestId, int month, int day)
    {
        if (!MyPageBirthdayController.IsValidDate(month, day)) throw new ArgumentOutOfRangeException(nameof(day));
        var buffer = new byte[10];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PROFILE_BIRTHDAY_SET).CopyTo(buffer, 2);
        BitConverter.GetBytes(requestId).CopyTo(buffer, 4);
        buffer[8] = (byte)month; buffer[9] = (byte)day;
        return buffer;
    }
    public bool SendProfileBirthday(uint requestId, int month, int day)
    {
        if (!IsConnected || !IsAuthenticated || !MyPageBirthdayController.IsValidDate(month, day)) return false;
        client.Send(BirthdayPacket(requestId, month, day));
        return true;
    }
    private void HandleProfileBirthdayResult(byte[] body)
    {
        if (body == null || body.Length != 7) return;
        MyPagePanelController.Instance?.GetComponent<MyPageBirthdayController>()?.ApplyResult(
            BitConverter.ToUInt32(body, 0), body[4], body[5], body[6]);
    }
}
