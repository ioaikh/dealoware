using System.Security.Cryptography;
using System.Text;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Tests;

/// <summary>R1: recovery codes are HMAC-SHA256 with a fail-closed server key.</summary>
public class AdminRecoveryCodeHasherTests
{
    [Fact]
    public void SecR1_Hash_IsHmacSha256NotBareSha256()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var hasher = new AdminRecoveryCodeHasher(key);
        var code = "ABCD-EFGH-2345";
        var hmac = hasher.Hash(code);
        var bare = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AdminRecoveryCodes.Normalize(code))));
        Assert.False(AdminRecoveryCodes.FixedTimeEquals(hmac, bare));
        Assert.True(hasher.FixedTimeEquals(hmac, code));
        Assert.True(hasher.FixedTimeEquals(hmac, "abcd efgh 2345"));
        Assert.False(hasher.FixedTimeEquals(hmac, "AAAA-BBBB-CCCC"));
    }

    [Fact]
    public void SecR1_Create_FailsWhenKeyMissing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => AdminRecoveryCodeHasher.Create(_ => null));
        Assert.Contains(AdminRecoveryCodeHasher.KeyEnvironmentVariable, ex.Message);
    }

    [Fact]
    public void SecR1_Create_FailsWhenKeyTooShort()
    {
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var ex = Assert.Throws<InvalidOperationException>(() => AdminRecoveryCodeHasher.Create(_ => shortKey));
        Assert.Contains("32", ex.Message);
    }
}
