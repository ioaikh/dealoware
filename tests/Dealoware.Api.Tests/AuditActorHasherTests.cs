using System.Security.Cryptography;
using System.Text;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Tests;

public class AuditActorHasherTests
{
    [Fact]
    public void SecN8_AuditActorHasher_KnownAnswerPinsInfoLabel()
    {
        Assert.Equal("dw-admin-audit-email-v1", AuditActorHasher.InfoLabel);

        var ikm = new byte[32];
        Array.Fill(ikm, (byte)0x0b);
        var hasher = new AuditActorHasher(ikm);
        const string expected = "o6R2yf9WpmPXM2EnYmYrJBkvaCSU9m+DTOYseey0KZ4=";
        Assert.Equal(expected, hasher.HashEmail("user@example.com"));

        var derived = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ikm,
            32,
            salt: [],
            info: Encoding.UTF8.GetBytes(AuditActorHasher.InfoLabel));
        using var hmac = new HMACSHA256(derived);
        Assert.Equal(
            expected,
            Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes("user@example.com"))));

        var otherInfo = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ikm,
            32,
            salt: [],
            info: Encoding.UTF8.GetBytes("dw-admin-audit-email-v0"));
        using var otherHmac = new HMACSHA256(otherInfo);
        Assert.NotEqual(
            expected,
            Convert.ToBase64String(otherHmac.ComputeHash(Encoding.UTF8.GetBytes("user@example.com"))));
    }
}
