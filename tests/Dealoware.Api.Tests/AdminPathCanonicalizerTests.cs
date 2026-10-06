using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

/// <summary>r3 §10 C1 path canonicalisation before exemption compare.</summary>
public class AdminPathCanonicalizerTests
{
    [Theory]
    [InlineData("/admin/sign-in", "/admin/sign-in")]
    [InlineData("/admin/sign-in/code", "/admin/sign-in/code")]
    [InlineData("/admin/", "/admin/")]
    [InlineData("/ADMIN/SIGN-IN", "/ADMIN/SIGN-IN")]
    public void TdAdm023_C1_AcceptsExactLockedPaths(string input, string expected)
    {
        Assert.True(AdminPathCanonicalizer.TryCanonicalize(input, out var canonical));
        Assert.Equal(expected, canonical);
    }

    [Theory]
    [InlineData("/admin/sign-in/")]
    [InlineData("/admin/sign-in/code/")]
    [InlineData("/admin/setup/authenticator/")]
    [InlineData("/admin//sign-in")]
    [InlineData("/admin/sign-in/../sign-in")]
    [InlineData("/admin/sign-in;foo")]
    [InlineData("/admin/sign-in%2fcode")]
    [InlineData("/admin/sign-in%2Fcode")]
    [InlineData("/admin/sign-in%5c")]
    [InlineData("/admin/sign-in%252f")]
    [InlineData("/admin/sign-in\\code")]
    [InlineData("/admin/sign-in\u0000")]
    public void TdAdm023_C1_RejectsNonCanonicalPaths(string input)
    {
        Assert.False(AdminPathCanonicalizer.TryCanonicalize(input, out _));
    }
}
