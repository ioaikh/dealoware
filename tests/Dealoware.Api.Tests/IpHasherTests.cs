using System.Security.Cryptography;
using System.Text;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// TD-ADM-053: keyed HMAC-SHA256 of client IP. Keys are generated at runtime.
/// </summary>
public class IpHasherTests
{
    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void TdAdm053_SameIpAndKey_ProducesStableKeyedHmac()
    {
        var hasher = new IpHasher(NewKey());
        var first = hasher.Hash("203.0.113.10");
        var second = hasher.Hash("203.0.113.10");

        Assert.Equal(first, second);
        Assert.NotEqual("203.0.113.10", first);
        Assert.DoesNotContain("203.0.113.10", first);
    }

    [Fact]
    public void TdAdm053_DifferentIps_ProduceDifferentHashes()
    {
        var hasher = new IpHasher(NewKey());
        Assert.NotEqual(hasher.Hash("203.0.113.10"), hasher.Hash("203.0.113.11"));
    }

    [Fact]
    public void TdAdm053_SameIpDifferentKeys_ProduceDifferentHashes()
    {
        var left = new IpHasher(NewKey()).Hash("198.51.100.7");
        var right = new IpHasher(NewKey()).Hash("198.51.100.7");
        Assert.NotEqual(left, right);
    }

    [Fact]
    public void TdAdm053_HashIsNotUnkeyedSha256()
    {
        var key = NewKey();
        var hasher = new IpHasher(key);
        var hmac = hasher.Hash("192.0.2.1");

        var unkeyed = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("192.0.2.1")));
        Assert.NotEqual(unkeyed, hmac);
    }

    [Fact]
    public void TdAdm053_EmptyIp_Throws()
    {
        var hasher = new IpHasher(NewKey());
        Assert.Throws<ArgumentException>(() => hasher.Hash(""));
        Assert.Throws<ArgumentException>(() => hasher.Hash(null!));
    }

    [Fact]
    public void TdAdm053_ShortKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new IpHasher(new byte[16]));
    }

    [Fact]
    public void TdAdm053_MissingKey_NonDevelopment_FailClosed()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => IpHasher.Create(_ => null, isDevelopment: false));
        Assert.Contains(IpHasher.KeyEnvironmentVariable, ex.Message);
        Assert.DoesNotContain("203.0.113", ex.Message);
    }

    [Fact]
    public void TdAdm053_MissingKey_Development_UsesEphemeralKey()
    {
        var hasher = IpHasher.Create(_ => null, isDevelopment: true);
        var hash = hasher.Hash("192.0.2.99");
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.DoesNotContain("192.0.2.99", hash);
    }

    [Fact]
    public void TdAdm053_Create_UsesProvidedRuntimeKey()
    {
        var key = NewKey();
        var fromFactory = IpHasher.Create(_ => key, isDevelopment: false);
        var direct = new IpHasher(key);
        Assert.Equal(direct.Hash("198.51.100.20"), fromFactory.Hash("198.51.100.20"));
    }
}
