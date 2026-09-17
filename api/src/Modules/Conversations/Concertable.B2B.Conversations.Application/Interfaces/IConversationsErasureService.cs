namespace Concertable.B2B.Conversations.Application.Interfaces;

/// <summary>The Conversations module's GDPR-erasure operations for a subject: severing the author link on the
/// messages they sent and pseudonymising the participant profiles of wound-down tenants. Admin-operated over the
/// unfiltered privileged stance.</summary>
internal interface IConversationsErasureService
{
    /// <summary>SEVER: drops the personal author link on every message the subject sent, keeping the body.</summary>
    Task SeverAuthoredMessagesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Pseudonymises the participant-profile display identity of the given (wound-down) tenants — a
    /// projection copy of an erased sole trader's name/address.</summary>
    Task ScrubParticipantProfilesAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
