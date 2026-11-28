namespace Dealoware.Api.Tests;

/// <summary>
/// The owner address the test hosts use: a placeholder on a reserved test domain. The real
/// owner address is supplied by the environment in deployment and never written in the
/// repository (T59 spec §2.9, answer 11).
/// </summary>
public static class TestCoreOwner
{
    public const string Email = "core-owner@example.test";

    /// <summary>The configuration key the environment fills as <c>CoreOwner__Email</c>.</summary>
    public const string ConfigurationKey = "CoreOwner:Email";
}
