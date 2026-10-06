namespace Dealoware.Api.Admin;

/// <summary>
/// r3 §10 C5: admin-specific cookie names, no __Host- prefix (Path=/admin).
/// </summary>
public static class AdminCookieNames
{
    public const string Session = "dw_admin_session";
    public const string Pending = "dw_admin_pending";
    public const string AntiForgery = "dw_admin_af";
    public const string AntiForgeryHeader = "X-Admin-AF";
}
