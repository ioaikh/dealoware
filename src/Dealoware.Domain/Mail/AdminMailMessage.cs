namespace Dealoware.Domain.Mail;

/// <summary>
/// Mail payload for the admin mail port. Callers supply destination, purpose, and text.
/// Values must not include password, TOTP, recovery, or signing material.
/// </summary>
public sealed class AdminMailMessage
{
    public const int MaxToLength = 320;
    public const int MaxSubjectLength = 200;
    public const int MaxBodyLength = 8192;

    public string To { get; }
    public MailPurpose Purpose { get; }
    public string Subject { get; }
    public string TextBody { get; }

    public AdminMailMessage(string to, MailPurpose purpose, string subject, string textBody)
    {
        To = MailHeaderText.NormalizeSingleAddress(to, MaxToLength, nameof(to));
        if (!Enum.IsDefined(purpose))
            throw new ArgumentOutOfRangeException(nameof(purpose));
        Subject = MailHeaderText.NormalizeRequired(subject, MaxSubjectLength, nameof(subject));
        if (string.IsNullOrWhiteSpace(textBody) || textBody.Length > MaxBodyLength)
            throw new ArgumentException("Body is required and must be 8192 characters or fewer.", nameof(textBody));

        Purpose = purpose;
        TextBody = textBody;
    }
}
