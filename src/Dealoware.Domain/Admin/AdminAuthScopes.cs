namespace Dealoware.Domain.Admin;

/// <summary>
/// Lockout / throttle subject scopes. Raw IP is allowed only on the IP scopes.
/// </summary>
public static class AdminAuthScopes
{
    public const string Account = "account";
    public const string LoginIp = "login_ip";
    public const string ResetIp = "reset_ip";
}
