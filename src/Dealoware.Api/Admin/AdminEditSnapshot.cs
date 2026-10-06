using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;

namespace Dealoware.Api.Admin;

/// <summary>
/// FieldPolicy-allowed, non-secret before/after snapshots for admin edits.
/// Values over 4 KiB are truncated with original length and SHA-256.
/// </summary>
internal static class AdminEditSnapshot
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Participant(Participant participant) =>
        Finish(new Dictionary<string, object?>
        {
            ["id"] = participant.Id,
            ["sub"] = participant.Sub,
            ["displayName"] = participant.DisplayName,
            ["isActive"] = participant.IsActive,
            ["version"] = participant.Version,
            ["updatedAt"] = participant.UpdatedAt
        });

    public static string Artifact(Artifact artifact) =>
        Finish(new Dictionary<string, object?>
        {
            ["id"] = artifact.Id,
            ["name"] = artifact.Entities.FirstOrDefault()?.Name,
            ["description"] = artifact.Entities.FirstOrDefault()?.Description,
            ["ownerParticipantId"] = artifact.OwnerParticipantId,
            ["version"] = artifact.Version,
            ["updatedAt"] = artifact.UpdatedAt
        });

    public static string Negotiation(Negotiation negotiation) =>
        Finish(new Dictionary<string, object?>
        {
            ["id"] = negotiation.Id,
            ["status"] = negotiation.Status.ToString(),
            ["endsAt"] = negotiation.EndsAt,
            ["startsAt"] = negotiation.StartsAt,
            ["version"] = negotiation.Version,
            ["updatedAt"] = negotiation.UpdatedAt
        });

    public static string Offer(Offer offer) =>
        Finish(new Dictionary<string, object?>
        {
            ["id"] = offer.Id,
            ["status"] = offer.Status.ToString(),
            ["amount"] = offer.Amount,
            ["currency"] = offer.Currency,
            ["terms"] = offer.Terms,
            ["version"] = offer.Version,
            ["updatedAt"] = offer.UpdatedAt
        });

    private static string Finish(Dictionary<string, object?> fields)
    {
        var json = JsonSerializer.Serialize(fields, JsonOptions);
        if (json.Length <= 4096)
            return json;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var suffix = $"... [TRUNCATED, original length: {json.Length}, sha256: {hash}]";
        var keep = Math.Max(0, 4096 - suffix.Length);
        return json[..keep] + suffix;
    }
}
