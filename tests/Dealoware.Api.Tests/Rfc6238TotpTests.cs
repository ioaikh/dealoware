using System.Text;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Tests;

/// <summary>RFC 6238 Appendix B vectors, truncated to 6 digits.</summary>
public class Rfc6238TotpTests
{
    private static readonly byte[] RfcSecret = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(59, "287082")]
    [InlineData(1111111109, "081804")]
    [InlineData(1111111111, "050471")]
    [InlineData(1234567890, "005924")]
    [InlineData(2000000000, "279037")]
    public void Rfc6238_Sha1_SixDigits_MatchesAppendixB(long unixSeconds, string expectedSix)
    {
        var timestep = unixSeconds / Rfc6238Totp.PeriodSeconds;
        Assert.Equal(expectedSix, Rfc6238Totp.ComputeCode(RfcSecret, timestep));
    }

    [Fact]
    public void TryVerify_AcceptsCurrentStepAndRejectsFarSkew()
    {
        var secret = Rfc6238Totp.GenerateSecret();
        var now = DateTimeOffset.FromUnixTimeSeconds(1_111_111_111);
        var code = Rfc6238Totp.ComputeCode(secret, Rfc6238Totp.TimestepAt(now));
        Assert.True(Rfc6238Totp.TryVerify(secret, code, now, lastUsedTimestep: null, out _));

        var far = now.AddSeconds(Rfc6238Totp.PeriodSeconds * 3);
        Assert.False(Rfc6238Totp.TryVerify(secret, code, far, lastUsedTimestep: null, out _));
    }

    [Fact]
    public void TryVerify_RejectsReplayOfLastUsedTimestep()
    {
        var secret = Rfc6238Totp.GenerateSecret();
        var now = DateTimeOffset.UtcNow;
        var step = Rfc6238Totp.TimestepAt(now);
        var code = Rfc6238Totp.ComputeCode(secret, step);
        Assert.True(Rfc6238Totp.TryVerify(secret, code, now, lastUsedTimestep: null, out var matched));
        Assert.Equal(step, matched);
        Assert.False(Rfc6238Totp.TryVerify(secret, code, now, lastUsedTimestep: matched, out _));
    }
}
