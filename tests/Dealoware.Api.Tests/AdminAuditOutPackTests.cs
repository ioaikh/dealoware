using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// Static out-pack: viewer sources never ship secrets, raw IPs, mutate controls, or anonymous assets.
/// </summary>
public class AdminAuditOutPackTests
{
    [Fact]
    public void TD_ADM_150_TD_ADM_053_UiSources_HaveNoSecretsRawIpsOrMutators()
    {
        var files = new[]
        {
            AdminAuditViewerEndpoints.LoadUiResource("audit-list.html"),
            AdminAuditViewerEndpoints.LoadUiResource("audit-detail.html"),
            AdminAuditViewerEndpoints.LoadUiResource("audit.css"),
            AdminAuditViewerEndpoints.LoadUiResource("audit.js")
        };

        foreach (var source in files)
        {
            Assert.DoesNotContain("DEALOWARE_ADMIN_IP_HMAC_KEY", source);
            Assert.DoesNotContain("203.0.113.", source);
            Assert.DoesNotContain("passwordHash", source);
            Assert.DoesNotContain("innerHTML", source);
            Assert.DoesNotContain("document.write", source);
            Assert.DoesNotContain("contenteditable", source);
            Assert.DoesNotContain("Show deleted", source);
        }

        var list = files[0];
        var detail = files[1];
        Assert.Contains("data-admin-screen=\"S-D1\"", list);
        Assert.Contains("data-admin-screen=\"S-D2\"", detail);
        Assert.DoesNotContain("type=\"search\"", list);
        Assert.DoesNotContain("name=\"q\"", list);
        Assert.Contains("/admin/audit", list);
        Assert.Contains("/admin/audit", detail);
        Assert.DoesNotContain("href=\"/audit\"", list);
        Assert.DoesNotContain("href=\"/audit\"", detail);
    }

    [Fact]
    public void TD_ADM_UI_na_X01_AuditRoutes_AreAdminPrefixed_AndReturnPathsMatchR3()
    {
        Assert.Equal("/admin/audit", AdminAuditViewerEndpoints.ListPath);
        Assert.StartsWith("/admin/audit/", AdminAuditViewerEndpoints.ShellCssPath);
        Assert.StartsWith("/admin/audit/", AdminAuditViewerEndpoints.ShellJsPath);
        Assert.True(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/audit"));
        Assert.True(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/audit/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        Assert.False(AdminAuditViewerEndpoints.IsAllowedReturnPath("/admin/audit/shell/audit.js"));
        Assert.Equal("/admin", AdminSessionCookie.CreateOptions().Path);
    }
}
