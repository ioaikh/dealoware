using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dealoware.Domain.Admin;

namespace Dealoware.Infrastructure.Mail;

/// <summary>
/// SES adapter behind <see cref="IAdminMailSender"/>. Talks HTTP only — no AWS SDK package or SES SDK types.
/// Region and from-address come from environment at runtime. It signs with the service's own
/// role credentials (or, in Development only, fixed keys); no credential is ever logged.
/// </summary>
public sealed class SesMailSender : IAdminMailSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SesMailOptions _options;
    private readonly HttpClient _httpClient;
    private readonly IAwsCredentialSource _credentials;
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>
    /// Takes the credentials the options name. For the role credentials, the same client also
    /// fetches them; the service registration gives them a client of their own instead.
    /// </summary>
    public SesMailSender(SesMailOptions options, HttpClient httpClient, Func<DateTimeOffset>? clock = null)
        : this(
            options,
            httpClient,
            (options ?? throw new ArgumentNullException(nameof(options))).CreateCredentialSource(httpClient, clock),
            clock)
    {
    }

    public SesMailSender(
        SesMailOptions options,
        HttpClient httpClient,
        IAwsCredentialSource credentials,
        Func<DateTimeOffset>? clock = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        MailHeaderText.NormalizeSingleAddress(message.To, MailHeaderText.MaxToLength, nameof(message.To));
        MailHeaderText.NormalizeRequired(message.Subject, MailHeaderText.MaxSubjectLength, nameof(message.Subject));
        if (string.IsNullOrWhiteSpace(message.TextBody) || message.TextBody.Length > MailHeaderText.MaxBodyLength)
            throw new ArgumentException("Body is required and must be 8192 characters or fewer.", nameof(message.TextBody));

        var payloadObject = new SesHttpSendRequest
        {
            FromEmailAddress = _options.From,
            Destination = new SesHttpDestination { ToAddresses = new[] { message.To } },
            Content = new SesHttpContent
            {
                Simple = new SesHttpSimple
                {
                    Subject = new SesHttpText { Data = message.Subject, Charset = "UTF-8" },
                    Body = new SesHttpBody
                    {
                        Text = new SesHttpText { Data = message.TextBody, Charset = "UTF-8" }
                    }
                }
            }
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(payloadObject, JsonOptions);
        var uri = new Uri("https://email." + _options.Region + ".amazonaws.com/v2/email/outbound-emails");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Content = new ByteArrayContent(payload);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var credentials = await _credentials.GetCredentialsAsync(cancellationToken).ConfigureAwait(false);
        AwsSigV4Signer.SignPost(
            request,
            payload,
            credentials,
            _options.Region,
            service: "ses",
            nowUtc: _clock());

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new SesMailSendException((int)response.StatusCode);
        }
    }

    private sealed class SesHttpSendRequest
    {
        public required string FromEmailAddress { get; init; }
        public required SesHttpDestination Destination { get; init; }
        public required SesHttpContent Content { get; init; }
    }

    private sealed class SesHttpDestination
    {
        public required string[] ToAddresses { get; init; }
    }

    private sealed class SesHttpContent
    {
        public required SesHttpSimple Simple { get; init; }
    }

    private sealed class SesHttpSimple
    {
        public required SesHttpText Subject { get; init; }
        public required SesHttpBody Body { get; init; }
    }

    private sealed class SesHttpBody
    {
        public required SesHttpText Text { get; init; }
    }

    private sealed class SesHttpText
    {
        public required string Data { get; init; }
        public required string Charset { get; init; }
    }
}
