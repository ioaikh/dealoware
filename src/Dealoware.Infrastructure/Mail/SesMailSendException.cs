using System.Globalization;

namespace Dealoware.Infrastructure.Mail;

/// <summary>
/// SES refused a send. It carries the HTTP status as a number and nothing else: never the
/// address, the subject, the body, SES's own answer or a credential.
/// </summary>
public sealed class SesMailSendException : InvalidOperationException
{
    public SesMailSendException(int statusCode)
        : base("Mail send failed with HTTP " + statusCode.ToString(CultureInfo.InvariantCulture) + ".")
    {
        StatusCode = statusCode;
    }

    /// <summary>The HTTP status SES answered with.</summary>
    public int StatusCode { get; }
}
