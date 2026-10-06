using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// RFC 6238 TOTP: HMAC-SHA1, 30-second period, 6 digits, Unix epoch.
/// Parameters match the Spec/SA lock (6 digits from the sign-in steps note;
/// period and SHA-1 are the RFC 6238 defaults the SA left open).
/// </summary>
public static class Rfc6238Totp
{
    public const int Digits = 6;
    public const int PeriodSeconds = 30;
    public const int AllowedSkewSteps = 1;
    public const int SecretLengthBytes = 20;

    /// <summary>RFC 4648 base32 alphabet (no padding on encode of 20-byte secrets).</summary>
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static byte[] GenerateSecret()
    {
        var secret = new byte[SecretLengthBytes];
        RandomNumberGenerator.Fill(secret);
        return secret;
    }

    public static string ToBase32(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return string.Empty;

        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                output.Append(Base32Alphabet[(buffer >> bitsLeft) & 31]);
            }
        }

        if (bitsLeft > 0)
        {
            output.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }

        return output.ToString();
    }

    public static byte[] FromBase32(string encoded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encoded);
        var cleaned = encoded.Trim().TrimEnd('=').ToUpperInvariant();
        var buffer = 0;
        var bitsLeft = 0;
        var bytes = new List<byte>(cleaned.Length);
        foreach (var ch in cleaned)
        {
            var value = Base32Alphabet.IndexOf(ch);
            if (value < 0)
                throw new FormatException("Invalid base32 character.");

            buffer = (buffer << 5) | value;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                bytes.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return bytes.ToArray();
    }

    public static string ComputeCode(ReadOnlySpan<byte> secret, long timestep)
    {
        var counter = new byte[8];
        var value = timestep;
        for (var i = 7; i >= 0; i--)
        {
            counter[i] = (byte)(value & 0xFF);
            value >>= 8;
        }

        var hash = HMACSHA1.HashData(secret, counter);
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);
        var otp = binary % 1_000_000;
        return otp.ToString("D6");
    }

    public static long TimestepAt(DateTimeOffset utc)
        => utc.ToUnixTimeSeconds() / PeriodSeconds;

    private static int _evaluationCount;

    /// <summary>Test probe: TOTP/recovery/enrol evaluations started after the last reset.</summary>
    public static int EvaluationCount => Volatile.Read(ref _evaluationCount);

    public static void ResetEvaluationCount() => Interlocked.Exchange(ref _evaluationCount, 0);

    public static void RecordEvaluation() => Interlocked.Increment(ref _evaluationCount);

    public static bool TryVerify(
        ReadOnlySpan<byte> secret,
        string code,
        DateTimeOffset utc,
        long? lastUsedTimestep,
        out long matchedTimestep)
    {
        RecordEvaluation();
        matchedTimestep = 0;
        if (string.IsNullOrWhiteSpace(code) || code.Length != Digits)
            return false;

        foreach (var ch in code)
        {
            if (ch is < '0' or > '9')
                return false;
        }

        var center = TimestepAt(utc);
        for (var delta = -AllowedSkewSteps; delta <= AllowedSkewSteps; delta++)
        {
            var step = center + delta;
            if (lastUsedTimestep is { } used && used == step)
                continue;

            var expected = ComputeCode(secret, step);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expected),
                    Encoding.ASCII.GetBytes(code)))
            {
                matchedTimestep = step;
                return true;
            }
        }

        return false;
    }

    public static string BuildOtpAuthUri(string email, string base32Secret)
    {
        var label = Uri.EscapeDataString($"Dealoware:{email}");
        var issuer = Uri.EscapeDataString("Dealoware");
        return $"otpauth://totp/{label}?secret={base32Secret}&issuer={issuer}&digits={Digits}&period={PeriodSeconds}&algorithm=SHA1";
    }
}
