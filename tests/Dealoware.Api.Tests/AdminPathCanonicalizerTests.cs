using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

public class AdminPathCanonicalizerTests
{
    [Theory]
    [InlineData("/admin/reset", "/admin/reset")]
    [InlineData("/admin/reset/confirm", "/admin/reset/confirm")]
    [InlineData("/admin/auth/reset.css", "/admin/auth/reset.css")]
    [InlineData("/admin/%72eset", "/admin/reset")]
    [InlineData("/admin/reset%2fextra", "/admin/reset/extra")]
    [InlineData("/admin/reset/", "/admin/reset/")]
    public void C1_Accepts_ThenExactCompare_OwnsTrailingSlashAndExtraSegment(string raw, string expected)
    {
        Assert.True(AdminPathCanonicalizer.TryCanonicalize(raw, out var canonical));
        Assert.Equal(expected, canonical);
    }

    [Theory]
    [InlineData("/admin/reset%252f")]
    [InlineData("/admin/reset%255c")]
    [InlineData("/admin/reset%252e")]
    [InlineData("/admin/reset%2525")]
    [InlineData("/admin/reset%5c")]
    [InlineData("/admin/%2e%2e/reset")]
    [InlineData("/admin/reset\\sent")]
    [InlineData("/admin/reset/../sent")]
    [InlineData("/admin//reset")]
    [InlineData("/admin/reset;jsessionid=1")]
    [InlineData("/admin/reset/\u0000")]
    [InlineData("/admin/reset/\u200b")]
    public void C1_Rejects_ResidualEncoding_DotDot_SlashSlash_Matrix_Controls(string raw)
    {
        Assert.False(AdminPathCanonicalizer.TryCanonicalize(raw, out _));
    }

    [Fact]
    public void C8_AuthStatic_RequiresFileExtension()
    {
        Assert.True(AdminSignedOutExemptions.IsAuthStaticFile("/admin/auth/reset.css"));
        Assert.True(AdminSignedOutExemptions.IsAuthStaticFile("/admin/auth/img/logo.png"));
        Assert.False(AdminSignedOutExemptions.IsAuthStaticFile("/admin/auth/"));
        Assert.False(AdminSignedOutExemptions.IsAuthStaticFile("/admin/auth"));
        Assert.False(AdminSignedOutExemptions.IsAuthStaticFile("/admin/auth/handler"));
        Assert.False(AdminSignedOutExemptions.IsAuthStaticFile("/admin/authentication/reset.css"));
    }
}
