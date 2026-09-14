namespace Concertable.B2B.Conversations.Application.Interfaces;

/// <summary>The Conversations module's GDPR-erasure operations for a subject: severing the author link on the
/// messages they sent, pseudonymising the participant profiles of wound-down tenants, and exporting the message
/// bodies they authored. Admin-operated over the unfiltered privileged stance.</summary>
internal interface IConversationsErasureService
{
    /// <summary>SEVER: drops the personal author link on every message the subject sent, keeping the body.</summary>
    Task SeverAuthoredMessagesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Pseudonymises the participant-profile display identity of the given (wound-down) tenants — a
    /// projection copy of an erased sole trader's name/address.</summary>
    Task ScrubParticipantProfilesAsync(IReadOnlyList<Guid> tenantIds, CancellationToken ct = default);

    /// <summary>The subject's portable messages fragment (GDPR arts. 15/20): the bodies they authored.</summary>
    Task<IReadOnlyList<MessageExport>> ExportMessagesAsync(Guid userId, CancellationToken ct = default);
}
