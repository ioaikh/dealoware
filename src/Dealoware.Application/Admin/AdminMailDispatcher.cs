using Dealoware.Domain.Admin;

namespace Dealoware.Application.Admin;

/// <summary>
/// Bootstrap and password-reset mail go through <see cref="IAdminMailSender"/> only.
/// The mail layer builds the token URL itself from a kind plus raw token
/// (route note r3 b079a814; fragment form b6194918 item 6).
/// </summary>
public sealed class AdminMailDispatcher
{
    public const string BootstrapSubject = "Core admin bootstrap";
    public const string PasswordResetSubject = "Core admin password reset";

    private readonly IAdminMailSender _sender;

    public AdminMailDispatcher(IAdminMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendAsync(string to, MailLinkKind kind, string token, CancellationToken cancellationToken = default)
    {
        var link = AdminMailPagePaths.Build(kind, token);
        var purpose = kind == MailLinkKind.Bootstrap ? MailPurpose.Bootstrap : MailPurpose.PasswordReset;
        var subject = purpose == MailPurpose.Bootstrap ? BootstrapSubject : PasswordResetSubject;
        var lead = purpose == MailPurpose.Bootstrap
            ? "Use this one-time link to finish Core admin bootstrap."
            : "Use this one-time link to reset the Core admin password.";

        return _sender.SendAsync(
            MailHeaderText.CreateMessage(to, subject, lead + "\n\n" + link + "\n"),
            cancellationToken);
    }

    public Task SendBootstrapLinkAsync(string to, string token, CancellationToken cancellationToken = default)
        => SendAsync(to, MailLinkKind.Bootstrap, token, cancellationToken);

    public Task SendPasswordResetLinkAsync(string to, string token, CancellationToken cancellationToken = default)
        => SendAsync(to, MailLinkKind.Reset, token, cancellationToken);
}
