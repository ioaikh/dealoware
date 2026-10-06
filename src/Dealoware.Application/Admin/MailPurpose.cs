namespace Dealoware.Application.Admin;

/// <summary>
/// Why a Core admin mail is being sent. Dispatcher-side only; not part of the shared port record.
/// </summary>
public enum MailPurpose
{
    Bootstrap = 1,
    PasswordReset = 2
}
