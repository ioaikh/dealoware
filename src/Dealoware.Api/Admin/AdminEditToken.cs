using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;

namespace Dealoware.Api.Admin;

/// <summary>
/// The <c>ETag</c> of an admin detail read, and the <c>If-Match</c> an admin
/// write must send back (Spec §5: optimistic concurrency, stale write 409).
/// <para>
/// It names the state a change was prepared against: the row's
/// <c>Version</c>, and with it the parts of the row that a party's own action
/// changes without moving <c>Version</c> (status, the time of the last
/// change, and the values the admin can overwrite). A token of Version alone
/// would still match after a party accepted an offer, and the admin's cancel
/// would then be saved over the acceptance.
/// </para>
/// <para>
/// The form is <c>"{version}-{16 hex digits}"</c>. The digits are a hash, so
/// the header carries no field value. It is a comparison value, not a secret.
/// </para>
/// </summary>
public static partial class AdminEditToken
{
    public static string For(Participant participant)
        => Build(
            participant.Version,
            participant.UpdatedAt,
            participant.DeletedAt,
            [participant.IsActive ? "1" : "0", participant.DisplayName]);

    /// <summary>Needs the artifact's subject entities loaded.</summary>
    public static string For(Artifact artifact)
    {
        var state = new List<string?> { artifact.OwnerParticipantId };
        // By id: the order rows come back in is not fixed.
        foreach (var entity in artifact.Entities.OrderBy(e => e.Id))
        {
            state.Add(entity.Id.ToString("N"));
            state.Add(entity.Name);
            state.Add(entity.Description);
        }

        return Build(artifact.Version, artifact.UpdatedAt, artifact.DeletedAt, state);
    }

    public static string For(Negotiation negotiation)
        => Build(
            negotiation.Version,
            negotiation.UpdatedAt,
            negotiation.DeletedAt,
            [Number((int)negotiation.Status), Instant(negotiation.EndsAt)]);

    public static string For(Offer offer)
        => Build(offer.Version, offer.UpdatedAt, offer.DeletedAt, [Number((int)offer.Status)]);

    /// <summary>The token as an <c>ETag</c> header value: a strong validator, in quotes.</summary>
    public static string ToHeader(string token) => $"\"{token}\"";

    public enum Presented
    {
        /// <summary>No <c>If-Match</c> at all: 428.</summary>
        Missing,

        /// <summary>An <c>If-Match</c> that is not one token of ours (<c>*</c>, a list, another shape): 400.</summary>
        Malformed,

        Ok
    }

    /// <summary>
    /// Reads the one token of an <c>If-Match</c>. <c>*</c> and lists are
    /// refused: a write must name the one state it was prepared against. A
    /// <c>W/</c> prefix is dropped, because a proxy that compresses a reply
    /// may add it to the <c>ETag</c> the page then sends back; the value is
    /// compared whole either way.
    /// </summary>
    public static Presented Read(HttpRequest request, out string token)
    {
        token = string.Empty;
        var values = request.Headers.IfMatch;
        if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
            return Presented.Missing;
        if (values.Count != 1)
            return Presented.Malformed;

        var text = values[0]!.Trim();
        if (text.StartsWith("W/", StringComparison.Ordinal))
            text = text[2..];
        if (text.Length < 2 || text[0] != '"' || text[^1] != '"')
            return Presented.Malformed;

        text = text[1..^1];
        if (!Shape().IsMatch(text))
            return Presented.Malformed;

        token = text;
        return Presented.Ok;
    }

    public static bool Matches(string presented, string current)
        => string.Equals(presented, current, StringComparison.Ordinal);

    private static string Build(
        uint version,
        DateTimeOffset? updatedAt,
        DateTimeOffset? deletedAt,
        IEnumerable<string?> state)
    {
        var text = new StringBuilder();
        Append(text, Number(version));
        Append(text, Instant(updatedAt));
        Append(text, Instant(deletedAt));
        foreach (var part in state)
            Append(text, part);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()));
        return $"{Number(version)}-{Convert.ToHexStringLower(hash.AsSpan(0, 8))}";
    }

    /// <summary>Each part with its length, so two parts never read as one; null is not the empty text.</summary>
    private static void Append(StringBuilder text, string? part)
    {
        if (part is null)
        {
            text.Append("-;");
            return;
        }

        text.Append(part.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(part).Append(';');
    }

    private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// To the microsecond: what PostgreSQL keeps of a time. A finer value would
    /// give one token before the row is stored and another after it is read back.
    /// </summary>
    private static string? Instant(DateTimeOffset? at)
        => at is null ? null : Number(at.Value.UtcTicks / 10);

    [GeneratedRegex("^[0-9]{1,10}-[0-9a-f]{16}$", RegexOptions.CultureInvariant)]
    private static partial Regex Shape();
}
