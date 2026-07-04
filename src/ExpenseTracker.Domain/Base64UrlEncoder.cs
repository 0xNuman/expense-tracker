using System;

namespace ExpenseTracker.Domain;

/// <summary>Base64url (RFC 4648 §5) encoder/decoder for URL-safe token transport.</summary>
internal static class Base64UrlEncoder
{
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        var base64 = Convert.ToBase64String(bytes);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static byte[] DecodeBytes(string input)
    {
        if (string.IsNullOrEmpty(input)) throw new ArgumentException("Input is required.", nameof(input));
        var padded = input.Replace('-', '+').Replace('_', '/');
        var remainder = padded.Length % 4;
        if (remainder > 0) padded = padded.PadRight(padded.Length + (4 - remainder), '=');
        return Convert.FromBase64String(padded);
    }

    public static string EncodeString(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        return Encode(bytes);
    }

    public static string DecodeString(string input)
    {
        var bytes = DecodeBytes(input);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}