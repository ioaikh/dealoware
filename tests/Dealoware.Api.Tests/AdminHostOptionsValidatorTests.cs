using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// A11 items 3 and 4: outside Development, host validation must stay on and the
/// allowlist must be exactly admin.core.dealoware.com.
/// </summary>
public class AdminHostOptionsValidatorTests
{
    [Fact]
    public void Development_AllowsEnforceHostValidationFalse()
    {
        var options = new AdminHostOptions { EnforceHostValidation = false };

        var ex = Record.Exception(() => AdminHostOptionsValidator.Validate(options, isDevelopment: true));

        Assert.Null(ex);
    }

    [Fact]
    public void Development_AllowsExtendedAllowlist()
    {
        var options = new AdminHostOptions
        {
            EnforceHostValidation = true,
            AllowedHosts = ["admin.core.dealoware.com", "localhost"]
        };

        var ex = Record.Exception(() => AdminHostOptionsValidator.Validate(options, isDevelopment: true));

        Assert.Null(ex);
    }

    [Fact]
    public void Production_DefaultOptions_Pass()
    {
        var ex = Record.Exception(() =>
            AdminHostOptionsValidator.Validate(new AdminHostOptions(), isDevelopment: false));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData("admin.core.dealoware.com")]
    [InlineData("ADMIN.CORE.DEALOWARE.COM")]
    public void Production_ExactAllowlist_CaseInsensitive_Passes(string host)
    {
        var options = new AdminHostOptions
        {
            EnforceHostValidation = true,
            AllowedHosts = [host]
        };

        var ex = Record.Exception(() => AdminHostOptionsValidator.Validate(options, isDevelopment: false));

        Assert.Null(ex);
    }

    [Fact]
    public void Production_EnforceHostValidationFalse_Throws()
    {
        var options = new AdminHostOptions { EnforceHostValidation = false };

        var ex = Assert.Throws<InvalidOperationException>(
            () => AdminHostOptionsValidator.Validate(options, isDevelopment: false));

        Assert.Equal(AdminHostOptionsValidator.EnforceHostValidationMessage, ex.Message);
        Assert.DoesNotContain("stack", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_ExtraAllowedHost_Throws()
    {
        var options = new AdminHostOptions
        {
            AllowedHosts = ["admin.core.dealoware.com", "evil.example"]
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => AdminHostOptionsValidator.Validate(options, isDevelopment: false));

        Assert.Equal(AdminHostOptionsValidator.AllowedHostsMessage, ex.Message);
        Assert.DoesNotContain("evil.example", ex.Message);
    }

    [Fact]
    public void Production_ReplacementAllowedHost_Throws()
    {
        var options = new AdminHostOptions
        {
            AllowedHosts = ["raw.alb.example"]
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => AdminHostOptionsValidator.Validate(options, isDevelopment: false));

        Assert.Equal(AdminHostOptionsValidator.AllowedHostsMessage, ex.Message);
        Assert.DoesNotContain("raw.alb.example", ex.Message);
    }

    [Fact]
    public void Production_EmptyAllowlist_Throws()
    {
        var options = new AdminHostOptions { AllowedHosts = [] };

        var ex = Assert.Throws<InvalidOperationException>(
            () => AdminHostOptionsValidator.Validate(options, isDevelopment: false));

        Assert.Equal(AdminHostOptionsValidator.AllowedHostsMessage, ex.Message);
    }

    [Fact]
    public void Production_NullOptions_UsesDefaultsAndPasses()
    {
        var ex = Record.Exception(() => AdminHostOptionsValidator.Validate(null, isDevelopment: false));

        Assert.Null(ex);
    }

    [Fact]
    public void Staging_EnforceHostValidationFalse_Throws()
    {
        var options = new AdminHostOptions { EnforceHostValidation = false };

        var ex = Assert.Throws<InvalidOperationException>(
            () => AdminHostOptionsValidator.Validate(options, isDevelopment: false));

        Assert.Equal(AdminHostOptionsValidator.EnforceHostValidationMessage, ex.Message);
    }
}
