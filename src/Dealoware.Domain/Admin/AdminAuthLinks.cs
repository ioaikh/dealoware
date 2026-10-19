namespace Dealoware.Domain.Admin;

/// <summary>
/// Fixed-origin fragment links. The raw token never appears in a path or query.
/// </summary>
public static class AdminAuthLinks
{
    public const string Origin = "https://admin.core.dealoware.com";
    public const string ResetConfirmPath = "/admin/reset/confirm";
    public const string BootstrapPath = "/admin/bootstrap";
    public const string TokenFragmentPrefix = "#token=";

    public static string ResetConfirm(string rawToken) =>
        Origin + ResetConfirmPath + TokenFragmentPrefix + rawToken;

    public static string Bootstrap(string rawToken) =>
        Origin + BootstrapPath + TokenFragmentPrefix + rawToken;

    public static string? TryReadFragmentToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var index = value.IndexOf(TokenFragmentPrefix, StringComparison.Ordinal);
        if (index < 0)
        {
            return value.Trim();
        }

        return value[(index + TokenFragmentPrefix.Length)..].Trim();
    }
}
