using System.Text;
using Dealoware.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dealoware.Api.Tests;

/// <summary>
/// Production must refuse to start with a missing, blank, placeholder, or too-short JWT signing key.
/// Non-Production keeps the development placeholder fallback.
/// </summary>
public class JwtSigningKeyValidatorTests
{
    // Test-only values; not real secrets.
    private static readonly string Key31Bytes = new('k', 31);
    private static readonly string Key32Bytes = new('k', 32);
    private static readonly string Key64Bytes = new('z', 64);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Production_MissingOrBlankKey_Throws(string? key)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => JwtSigningKeyValidator.Validate(key, isProduction: true));

        Assert.Contains(JwtSigningKeyValidator.EnvironmentVariableName, ex.Message);
        Assert.Contains("not set", ex.Message);
    }

    [Theory]
    [InlineData(JwtSigningKeyValidator.DevelopmentPlaceholderKey)]
    [InlineData(" " + JwtSigningKeyValidator.DevelopmentPlaceholderKey + " ")]
    [InlineData("development_placeholder_key_change_in_production_32chars")]
    public void Production_PlaceholderKey_Throws_WithoutLeakingKey(string key)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => JwtSigningKeyValidator.Validate(key, isProduction: true));

        Assert.Contains("placeholder", ex.Message);
        Assert.DoesNotContain(JwtSigningKeyValidator.DevelopmentPlaceholderKey, ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CHANGE_IN_PRODUCTION", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_31ByteKey_Throws_WithoutLeakingKey()
    {
        Assert.Equal(31, Encoding.UTF8.GetByteCount(Key31Bytes));

        var ex = Assert.Throws<InvalidOperationException>(() => JwtSigningKeyValidator.Validate(Key31Bytes, isProduction: true));

        Assert.Contains("too short", ex.Message);
        Assert.DoesNotContain("kkkk", ex.Message);
    }

    [Fact]
    public void Production_KeyShortInBytes_CountsUtf8Bytes()
    {
        // 15 two-byte characters = 30 bytes even though 15 chars; 16 = 32 bytes.
        var thirtyBytes = new string('é', 15);
        var thirtyTwoBytes = new string('é', 16);

        Assert.Throws<InvalidOperationException>(() => JwtSigningKeyValidator.Validate(thirtyBytes, isProduction: true));
        Assert.Equal(thirtyTwoBytes, JwtSigningKeyValidator.Validate(thirtyTwoBytes, isProduction: true));
    }

    [Fact]
    public void Production_32ByteKey_Passes()
    {
        Assert.Equal(Key32Bytes, JwtSigningKeyValidator.Validate(Key32Bytes, isProduction: true));
    }

    [Fact]
    public void Production_LongKey_Passes()
    {
        Assert.Equal(Key64Bytes, JwtSigningKeyValidator.Validate(Key64Bytes, isProduction: true));
    }

    [Fact]
    public void NonProduction_MissingKey_FallsBackToPlaceholder()
    {
        Assert.Equal(
            JwtSigningKeyValidator.DevelopmentPlaceholderKey,
            JwtSigningKeyValidator.Validate(null, isProduction: false));
    }

    [Theory]
    [InlineData(JwtSigningKeyValidator.DevelopmentPlaceholderKey)]
    [InlineData("short")]
    [InlineData("")]
    public void NonProduction_KeepsProvidedValueUnchanged(string key)
    {
        Assert.Equal(key, JwtSigningKeyValidator.Validate(key, isProduction: false));
    }

    [Fact]
    public void ProductionHost_WithoutSigningKey_FailsAtStartup()
    {
        // Only meaningful when the runner has no real key configured.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(JwtSigningKeyValidator.EnvironmentVariableName)))
        {
            return;
        }

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var ex = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(ex);
        var messages = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        Assert.Contains(messages, m => m.Contains(JwtSigningKeyValidator.EnvironmentVariableName) && m.Contains("not set"));
    }
}
