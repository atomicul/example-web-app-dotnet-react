using System.Buffers.Binary;
using System.Security.Cryptography;

namespace TodoApi.Tests;

/// <summary>The codes an authenticator app shows for a shared key, as RFC 6238 computes them.</summary>
public static class Totp
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    /// <summary>The code the app shows now for the base32 key Identity's <c>POST /manage/2fa</c> answers with.</summary>
    public static string CodeFor(string sharedKey)
    {
        var counter = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)Step.TotalSeconds);
        var hash = HMACSHA1.HashData(FromBase32(sharedKey), counter);
        var offset = hash[^1] & 0x0f;
        var number = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset)) & int.MaxValue;
        return (number % 1_000_000).ToString("D6");
    }

    private static byte[] FromBase32(string text)
    {
        var bits = string.Concat(text.Select(character =>
            Convert.ToString(Base32Alphabet.IndexOf(char.ToUpperInvariant(character)), 2).PadLeft(5, '0')));
        return Enumerable.Range(0, bits.Length / 8).Select(index => Convert.ToByte(bits.Substring(index * 8, 8), 2)).ToArray();
    }
}
