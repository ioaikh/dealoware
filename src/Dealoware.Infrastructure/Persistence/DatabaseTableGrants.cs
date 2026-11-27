namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// H3, after every migration (design §7 step 6b): what the long-lived API login
/// (<see cref="DatabaseRoles.AppRole"/>) may do to each table of the model.
/// <para>
/// Every table in <see cref="DealowareDbContext"/> must be in exactly one of
/// the two lists. <c>DatabaseTableGrantsTests</c> fails when a table is added to
/// the model without being put in one of them, and <c>migrate</c> refuses a
/// database that holds a table in neither.
/// </para>
/// <list type="bullet">
///   <item><see cref="MayChange"/>: read, add, change and remove.</item>
///   <item><see cref="ReadAndAddOnly"/>: read and add; never change or remove.</item>
/// </list>
/// The EF Core history table is neither: the API may only read it.
/// </summary>
public static class DatabaseTableGrants
{
    /// <summary>The EF Core history table: the API may only read it.</summary>
    public const string HistoryTable = DatabaseMigrationBaseline.HistoryTableName;

    /// <summary>The admin audit log: the API may only read it and add to it.</summary>
    public const string AuditTable = "AdminAuditLog";

    /// <summary>Tables the API may read, add to, change and remove from.</summary>
    public static readonly IReadOnlyList<string> MayChange =
    [
        // Engine
        "AcceptGrants",
        "ApiKeyCredentials",
        "Artifacts",
        "ArtifactValues",
        "EntityProperties",
        "Negotiations",
        "Offers",
        "ParticipantBudgets",
        "Participants",
        "RevokedTokens",
        "Strategies",
        "SubjectEntities",
        "TimePeriods",

        // Admin console
        "AdminAuthFailureEvents",
        "AdminAuthLockouts",
        "AdminAuthTokens",
        "AdminBootstrapTokens",
        "AdminCoreOwnerAccounts",
        "AdminCredentials",
        "AdminDeleteConfirmTokens",
        "AdminPasswordResetTokens",
        "AdminPendingAuths",
        "AdminRecoveryCodes",
        "AdminResetIpCounters",
        "AdminSessionRecoveryNotices",
        "AdminSessions",
        "AdminSessionStepUps",
    ];

    /// <summary>Tables the API may read and add to, and never change or remove from.</summary>
    public static readonly IReadOnlyList<string> ReadAndAddOnly =
    [
        AuditTable,
    ];
}
