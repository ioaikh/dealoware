using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

/// <summary>What a request must bring for the session gate to let it through.</summary>
public enum AdminRouteClass
{
    /// <summary>Answers without a session. Every such path and method is on the gate's exemption list.</summary>
    SignedOut,

    /// <summary>Answers only with a sign-in in progress (a pending token). On the exemption list, with the pending token.</summary>
    SignInInProgress,

    /// <summary>Answers only to a full session of the owner.</summary>
    SignedIn,

    /// <summary>Mapped only in Development and Testing. Behind the gate like a signed-in route; absent in Production.</summary>
    TestOnly
}

/// <summary>
/// One route the app maps under /admin: its pattern exactly as the app maps
/// it, the methods mapped on it, and its class. A path whose methods are of
/// different classes has one row per class.
/// </summary>
/// <param name="Pattern">The route pattern as mapped, for example <c>/admin/api/offers/{id:guid}</c>.</param>
/// <param name="Methods">The HTTP methods mapped on it.</param>
/// <param name="Class">What the gate asks of a request to it.</param>
/// <param name="Screen">The Spec X01 screen this page is, for a page a browser opens; null otherwise.</param>
/// <param name="OnlyRefuses">Mapped only to answer 405 (the audit log's change methods, TD-ADM-101).</param>
/// <param name="DebugBuildOnly">Compiled into Debug builds only, and mapped there only with its config flag.</param>
public sealed record AdminRoute(
    string Pattern,
    string[] Methods,
    AdminRouteClass Class,
    string? Screen = null,
    bool OnlyRefuses = false,
    bool DebugBuildOnly = false)
{
    /// <summary>A page: anything under /admin but the JSON routes.</summary>
    public bool IsPage => !Pattern.StartsWith("/admin/api/", StringComparison.Ordinal);

    /// <summary>The route's one parameter, if it has one: a record id.</summary>
    public bool HasId => Pattern.Contains("{id:guid}", StringComparison.Ordinal);

    /// <summary>A path this route answers, with <paramref name="id"/> for its record id.</summary>
    public string PathFor(Guid id) => Pattern.Replace("{id:guid}", id.ToString("D"), StringComparison.Ordinal);

    /// <summary>A path this route answers, with an id no record has.</summary>
    public string SamplePath => PathFor(AdminRouteTable.NoSuchId);

    public override string ToString() => $"{string.Join(",", Methods)} {Pattern}";
}

/// <summary>
/// One folder of static files under /admin. Not routes: the denial tests
/// walk every file that is in the folder on disk.
/// </summary>
/// <param name="UrlPrefix">Where the folder is served, for example <c>/admin/assets</c>.</param>
/// <param name="Folder">The folder under src/Dealoware.Api.</param>
/// <param name="Class">SignedOut (served before the gate) or SignedIn (served after it).</param>
public sealed record AdminStaticFolder(string UrlPrefix, string Folder, AdminRouteClass Class);

/// <summary>
/// The one list of every route the app maps under /admin (T08 spec,
/// deliverable 1). <c>AdminRouteDenialTests</c> holds it against the app's own
/// endpoints both ways, so a new /admin route without a row here fails the
/// build, and so does a row whose route is gone. Every row then goes through
/// the denial checks of its class.
/// <para>
/// To add a route: add one line to <see cref="Routes"/> with the helper for
/// its class (<see cref="Page"/>, <see cref="Api"/>, <see cref="SignedOut"/>,
/// <see cref="InProgress"/>). A page a browser opens names its Spec X01
/// screen. Nothing else needs to change: the checks read the rows.
/// </para>
/// </summary>
public static class AdminRouteTable
{
    /// <summary>An id no record has.</summary>
    public static readonly Guid NoSuchId = new("0b6c1f0e-3a54-4c58-9f0e-6f1d2c3b4a59");

    private const string Get = "GET";
    private const string Post = "POST";
    private const string Patch = "PATCH";
    private const string Delete = "DELETE";
    private static readonly string[] ChangeMethods = ["POST", "PUT", "PATCH", "DELETE"];

    private static AdminRoute SignedOut(string pattern, string screen, params string[] methods)
        => new(pattern, methods, AdminRouteClass.SignedOut, screen);

    private static AdminRoute SignedOutPost(string pattern)
        => new(pattern, [Post], AdminRouteClass.SignedOut);

    private static AdminRoute InProgress(string pattern, string? screen, params string[] methods)
        => new(pattern, methods, AdminRouteClass.SignInInProgress, screen);

    private static AdminRoute Page(string pattern, string screen)
        => new(pattern, [Get], AdminRouteClass.SignedIn, screen);

    private static AdminRoute Api(string method, string pattern)
        => new(pattern, [method], AdminRouteClass.SignedIn);

    private static readonly string[] EntityTypes = ["participants", "artifacts", "negotiations", "offers"];

    public static readonly IReadOnlyList<AdminRoute> Routes =
    [
        // ---- Signed out: the exemption list (AdminSignedOutExemptions) ----
        SignedOut(AdminSignedOutExemptions.SignIn, "S-A1", Get, Post),
        SignedOut(AdminSignedOutExemptions.Bootstrap, "S-A4", Get, Post),
        SignedOut(AdminSignedOutExemptions.LinkExpired, "S-A5", Get),
        SignedOut(AdminSignedOutExemptions.Reset, "S-A8", Get, Post),
        SignedOut(AdminSignedOutExemptions.ResetSent, "S-A9", Get),
        SignedOut(AdminSignedOutExemptions.ResetConfirm, "S-A10", Get, Post),
        SignedOutPost(AdminSignedOutExemptions.SignOut),
        SignedOutPost(AdminSignedOutExemptions.ApiSignIn),
        SignedOutPost(AdminSignedOutExemptions.ApiBootstrap),
        SignedOutPost(AdminSignedOutExemptions.ApiReset),
        SignedOutPost(AdminSignedOutExemptions.ApiResetConfirm),
        SignedOutPost(AdminSignedOutExemptions.ApiResetConfirmCheck),
        SignedOutPost(AdminSignedOutExemptions.ApiSignOut),

        // ---- Sign-in in progress: the exemption list, with the pending token ----
        InProgress(AdminSignedOutExemptions.SignInCode, "S-A2", Get, Post),
        InProgress(AdminSignedOutExemptions.SignInRecovery, "S-A3", Get, Post),
        InProgress(AdminSignedOutExemptions.SetupAuthenticator, "S-A6", Get, Post),
        InProgress(AdminSignedOutExemptions.SetupRecoveryCodes, "S-A7", Get),
        InProgress(AdminSignedOutExemptions.ApiSignInCode, null, Post),
        InProgress(AdminSignedOutExemptions.ApiSignInRecovery, null, Post),

        // ---- Signed in: pages ----
        Page(AdminSignedInPage.Stats, "S-B1"),
        Page("/admin/participants", "S-B2"),
        Page("/admin/artifacts", "S-B3"),
        Page("/admin/negotiations", "S-B4"),
        Page("/admin/offers", "S-B5"),
        .. EntityTypes.Select(t => Page($"/admin/{t}/{{id:guid}}", "S-B6")),
        Page(AdminAuditPages.ListPath, "S-D1"),
        Page(AdminAuditPages.ListPath + "/{id:guid}", "S-D2"),
        Page(AdminSignedInPage.SecuritySettings, "S-A12"),

        // ---- Signed in: the JSON routes ----
        Api(Get, "/admin/api/me"),
        Api(Get, AdminSessionEndpoints.StatusPath),
        Api(Post, AdminSessionEndpoints.StaySignedInPath),
        Api(Get, "/admin/api/stats"),
        .. EntityTypes.Select(t => Api(Get, $"/admin/api/{t}")),
        .. EntityTypes.Select(t => Api(Get, $"/admin/api/{t}/{{id:guid}}")),
        .. EntityTypes.Select(t => Api(Patch, $"/admin/api/{t}/{{id:guid}}")),
        .. EntityTypes.Select(t => Api(Delete, $"/admin/api/{t}/{{id:guid}}")),
        .. EntityTypes.Select(t => Api(Post, $"/admin/api/{t}/{{id:guid}}/delete-intent")),
        Api(Get, "/admin/api/audit"),
        Api(Get, "/admin/api/audit/{id:guid}"),
        new("/admin/api/audit", ChangeMethods, AdminRouteClass.SignedIn, OnlyRefuses: true),
        new("/admin/api/audit/{id:guid}", ChangeMethods, AdminRouteClass.SignedIn, OnlyRefuses: true),
        Api(Get, AdminSecuritySettingsEndpoints.BasePath),
        Api(Get, AdminSecuritySettingsEndpoints.ActivityPath),
        Api(Post, AdminSecuritySettingsEndpoints.PasswordPath),
        Api(Post, AdminSecuritySettingsEndpoints.StepUpPath),
        Api(Post, AdminSecuritySettingsEndpoints.RecoveryCodesPath),
        Api(Post, AdminSecuritySettingsEndpoints.AuthenticatorPath),
        Api(Post, AdminSecuritySettingsEndpoints.AuthenticatorConfirmPath),
        Api(Post, AdminRecoveryNotice.DismissPath),

        // ---- Test only: Development and Testing; gone in Production ----
        new(AdminCsrfProbeEndpoint.Path, [Post], AdminRouteClass.TestOnly),
        new(AdminTestExceptionEndpoint.Path, [Get], AdminRouteClass.TestOnly, DebugBuildOnly: true)
    ];

    /// <summary>The two static folders under /admin, each served whole on one side of the gate.</summary>
    public static readonly IReadOnlyList<AdminStaticFolder> StaticFolders =
    [
        new(AdminSignedOutExemptions.AuthStaticPrefix, Path.Combine("wwwroot", "admin", "auth"), AdminRouteClass.SignedOut),
        new(AdminSignedInPage.AssetPrefix, AdminSignedInPage.AssetFolder, AdminRouteClass.SignedIn)
    ];

    /// <summary>
    /// The Spec's X01 screen inventory (test design TD-ADM-UI-na-X01 step 1:
    /// S-A1 to S-A12, S-B1 to S-B6, S-C1, Toggle, S-D1 to S-D4). Each screen
    /// is a page row of the table, or says here why it has no route of its own.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string?> SpecScreens = new Dictionary<string, string?>
    {
        ["S-A1"] = null, ["S-A2"] = null, ["S-A3"] = null, ["S-A4"] = null, ["S-A5"] = null, ["S-A6"] = null,
        ["S-A7"] = null, ["S-A8"] = null, ["S-A9"] = null, ["S-A10"] = null,
        ["S-A11"] = "Session ended: S-A1 opened at #ended by the gate (AdminSignInPages.SessionEndedLocation).",
        ["S-A12"] = null,
        ["S-B1"] = null, ["S-B2"] = null, ["S-B3"] = null, ["S-B4"] = null, ["S-B5"] = null, ["S-B6"] = null,
        ["S-C1"] = "The delete confirmation: a dialog on the list and detail pages, not an address of its own.",
        ["Toggle"] = "Show deleted: a query parameter of the four lists.",
        ["S-D1"] = null, ["S-D2"] = null,
        ["S-D3"] = "We can't find that page: the answer to a signed-in GET of a page path no route answers (UseAdminPageNotFound).",
        ["S-D4"] = "You can't do that: not built; no signed-in page is refused (T08 architecture step, question 2)."
    };
}
