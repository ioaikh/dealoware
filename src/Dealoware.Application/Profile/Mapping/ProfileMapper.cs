using Dealoware.Application.Profile.Dtos;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Participants;

namespace Dealoware.Application.Profile.Mapping;

/// <summary>
/// Maps Participant to ProfileResponse with field ACL projection.
/// Denied fields are omitted entirely from the response.
/// </summary>
public static class ProfileMapper
{
    /// <summary>
    /// Maps a Participant to ProfileResponse, applying field ACL policy.
    /// Denied fields are omitted (not returned, not even as null).
    /// </summary>
    public static ProfileResponse ToResponse(
        Participant participant,
        FieldPrincipal principal,
        IFieldPolicy fieldPolicy)
    {
        var context = FieldResourceContext.ForSelfProfile(participant.Sub);
        
        var canReadLoginEmail = fieldPolicy.Evaluate(
            principal, 
            FieldClass.LoginEmail, 
            FieldAction.Read, 
            context);
        
        var canReadContactEmail = fieldPolicy.Evaluate(
            principal, 
            FieldClass.ContactEmail, 
            FieldAction.Read, 
            context);
        
        var canReadDisplayName = fieldPolicy.Evaluate(
            principal, 
            FieldClass.DisplayName, 
            FieldAction.Read, 
            context);

        return new ProfileResponse
        {
            Sub = participant.Sub,
            DisplayName = canReadDisplayName ? participant.DisplayName : null,
            LoginEmail = canReadLoginEmail ? participant.LoginEmail : null,
            ContactEmail = canReadContactEmail ? participant.ContactEmail : null,
            IncludesLoginEmail = canReadLoginEmail,
            IncludesContactEmail = canReadContactEmail,
            CreatedAt = participant.CreatedAt
        };
    }
}
