using System;
using System.Text;
using UnityEngine;

public static class Packet
{
    static UTF8Encoding utf8 = new UTF8Encoding(false, false);

    public static void WriteFixedString(byte[] buffer, int offset, string str, int size)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        if (offset < 0 || size <= 0 || offset + size > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        byte[] bytes = Encoding.UTF8.GetBytes(str ?? "");

        int len = Math.Min(bytes.Length, size - 1);

        while (len > 0 && (bytes[len - 1] & 0xC0) == 0x80)
        {
            len--;
        }

        Array.Clear(buffer, offset, size);

        Array.Copy(bytes, 0, buffer, offset, len);

        buffer[offset + len] = 0;
    }

    public static string ReadFixedString(byte[] buffer, int offset, int size)
    {
        int len = 0;
        while (len < size && buffer[offset + len] != 0)
            len++;

        string result = Encoding.UTF8.GetString(buffer, offset, len);

        Debug.Log($"[STRING DECODE] offset={offset} size={size} len={len} result={result}");

        return result;
    }
}