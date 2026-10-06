namespace Dealoware.Domain.Mail;

/// <summary>
/// Why a Core admin mail is being sent. Bootstrap and password-reset only in this slice.
/// </summary>
public enum MailPurpose
{
    Bootstrap = 1,
    PasswordReset = 2
}
