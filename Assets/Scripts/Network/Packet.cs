using System;
using System.Text;
using UnityEngine;

public static class Packet
{
    private static readonly UTF8Encoding utf8 =
        new UTF8Encoding(false, false);

    public static void WriteFixedString(
        byte[] buffer,
        int offset,
        string str,
        int size)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        if (offset < 0 ||
            size <= 0 ||
            offset + size > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        Array.Clear(
            buffer,
            offset,
            size
        );

        string value = str ?? string.Empty;

        char[] chars =
            value.ToCharArray();

        Encoder encoder =
            utf8.GetEncoder();

        encoder.Convert(
            chars,
            0,
            chars.Length,
            buffer,
            offset,
            size - 1,
            true,
            out int charsUsed,
            out int bytesUsed,
            out bool completed
        );

        buffer[offset + bytesUsed] = 0;
    }

    public static string ReadFixedString(
        byte[] buffer,
        int offset,
        int size)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        if (offset < 0 ||
            size < 0 ||
            offset + size > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        int length = 0;

        while (length < size &&
               buffer[offset + length] != 0)
        {
            length++;
        }

        return utf8.GetString(
            buffer,
            offset,
            length
        );
    }
}