namespace Dealoware.Api.Admin;

/// <summary>
/// Server-side token checks for r3 §2.2 / §10 C2 extra conditions.
/// Steps 2–5 replace the Development/fail-closed stand-ins with store validation.
/// </summary>
public interface IAdminAuthTokenGate
{
    Task<bool> HasValidPendingSignInAsync(HttpContext context, CancellationToken cancellationToken);
    Task<bool> HasValidBootstrapTokenAsync(HttpContext context, CancellationToken cancellationToken);
    Task<bool> HasValidResetTokenAsync(HttpContext context, CancellationToken cancellationToken);
    Task<bool> HasEnrolPendingAsync(HttpContext context, CancellationToken cancellationToken);
}

public sealed class FailClosedAdminAuthTokenGate : IAdminAuthTokenGate
{
    public Task<bool> HasValidPendingSignInAsync(HttpContext context, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> HasValidBootstrapTokenAsync(HttpContext context, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> HasValidResetTokenAsync(HttpContext context, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> HasEnrolPendingAsync(HttpContext context, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}

/// <summary>
/// Development / test stand-in until Steps 2–5 land stores.
/// Pending token is cookie-only (C2). Enrol pending reuses dw_admin_pending=enrol (C5: no extra name).
/// Bootstrap/reset link tokens are not on GET (COMMON 942df099: fragment then POST body).
/// </summary>
public sealed class DevelopmentAdminAuthTokenGate : IAdminAuthTokenGate
{
    public Task<bool> HasValidPendingSignInAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var value = context.Request.Cookies[AdminAuthCookies.PendingSignIn];
        return Task.FromResult(
            !string.IsNullOrWhiteSpace(value)
            && !string.Equals(value, AdminAuthCookies.EnrolPendingValue, StringComparison.Ordinal));
    }

    public Task<bool> HasValidBootstrapTokenAsync(HttpContext context, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> HasValidResetTokenAsync(HttpContext context, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> HasEnrolPendingAsync(HttpContext context, CancellationToken cancellationToken) =>
        Task.FromResult(string.Equals(
            context.Request.Cookies[AdminAuthCookies.PendingSignIn],
            AdminAuthCookies.EnrolPendingValue,
            StringComparison.Ordinal));
}
