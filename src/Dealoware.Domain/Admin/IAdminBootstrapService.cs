namespace Dealoware.Domain.Admin;

public interface IAdminBootstrapService
{
    /// <summary>
    /// Mints a single-use bootstrap token (24h max), stores only the hash,
    /// and sends the /admin/setup/password link through <see cref="IAdminMailSender"/>.
    /// </summary>
    Task<AdminBootstrapIssueResult> IssueLinkAsync(
        string? ipHmac,
        CancellationToken cancellationToken = default);

    Task<AdminBootstrapInspectResult> InspectAsync(
        string? rawToken,
        CancellationToken cancellationToken = default);

    Task<AdminBootstrapSetPasswordResult> SetPasswordAsync(
        string? rawToken,
        string? password,
        string? turnstileToken,
        string? remoteIp,
        string? ipHmac,
        CancellationToken cancellationToken = default);

    bool HasPasswordSet();
}

public sealed record AdminBootstrapIssueResult(bool Sent, string? Error);

public sealed record AdminBootstrapInspectResult(bool Valid);

public abstract record AdminBootstrapSetPasswordResult
{
    public sealed record Succeeded : AdminBootstrapSetPasswordResult;

    public sealed record TurnstileFailed : AdminBootstrapSetPasswordResult;

    public sealed record InvalidLink : AdminBootstrapSetPasswordResult;

    public sealed record PasswordRejected(string Error) : AdminBootstrapSetPasswordResult;

    public sealed record AlreadySet : AdminBootstrapSetPasswordResult;
}
