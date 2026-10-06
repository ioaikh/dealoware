using System.Security.Cryptography;
using System.Text;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// One-time recovery codes: 10 codes, shown once, stored as SHA-256 of the
/// normalized form (case-insensitive, spaces and dashes ignored).
/// Count follows the Spec §8.3 recommendation (10).
/// </summary>
public static class AdminRecoveryCodes
{
    public const int Count = 10;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static IReadOnlyList<string> Generate()
    {
        var codes = new string[Count];
        for (var i = 0; i < Count; i++)
        {
            var chars = new char[12];
            for (var c = 0; c < chars.Length; c++)
            {
                chars[c] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }

            codes[i] = $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}-{new string(chars, 8, 4)}";
        }

        return codes;
    }

    public static string Normalize(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        var builder = new StringBuilder(code.Length);
        foreach (var ch in code)
        {
            if (ch is '-' or ' ' or '\t')
                continue;
            builder.Append(char.ToUpperInvariant(ch));
        }

        return builder.ToString();
    }

    public static string Hash(string code)
    {
        var normalized = Normalize(code);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    public static bool FixedTimeEquals(string leftHash, string rightHash)
    {
        if (string.IsNullOrEmpty(leftHash) || string.IsNullOrEmpty(rightHash))
            return false;

        var a = Encoding.ASCII.GetBytes(leftHash);
        var b = Encoding.ASCII.GetBytes(rightHash);
        if (a.Length != b.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
