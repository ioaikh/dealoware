using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Pending-token helpers for the Step 5 session gate.
/// </summary>
public static class AdminSessionExemptions
{
    public const string PendingCookieName = "dw_admin_pending";

    public static async Task<bool> HasValidTokenAsync(
        HttpContext context,
        string kind,
        IAdminAuthTokenStore tokens,
        IAdminClock clock,
        CancellationToken ct)
    {
        var raw = ReadPendingCookie(context, kind);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var row = await tokens.GetByHashAsync(AdminAuthService.HashToken(raw), ct).ConfigureAwait(false);
        return row is not null
               && string.Equals(row.Kind, kind, StringComparison.Ordinal)
               && row.IsUsable(clock.UtcNow);
    }

    private static string? ReadPendingCookie(HttpContext context, string kind)
    {
        if (kind != AdminAuthToken.KindPending)
        {
            return null;
        }

        var cookie = context.Request.Cookies[PendingCookieName];
        return string.IsNullOrWhiteSpace(cookie) ? null : cookie;
    }
}
