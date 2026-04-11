using System;
using System.Text;

public static class Packet
{
    public static void WriteFixedString(byte[] buffer, int offset, string str, int size)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str ?? "");
        int len = Math.Min(bytes.Length, size - 1);
        Array.Copy(bytes, 0, buffer, offset, len);
    }

    public static string ReadFixedString(byte[] buffer, int offset, int size)
    {
        int len = 0;
        while (len < size && buffer[offset + len] != 0)
            len++;

        return Encoding.UTF8.GetString(buffer, offset, len);
    }
}