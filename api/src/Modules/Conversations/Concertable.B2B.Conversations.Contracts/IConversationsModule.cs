namespace Concertable.B2B.Conversations.Contracts;

public interface IConversationsModule
{
    Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null);
    Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null);

    /// <summary>GDPR erasure (SEVER): drops the personal author link on every message the subject sent, keeping the
    /// body and sender tenant for the limitation/OSA window.</summary>
    Task SeverAuthoredMessagesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>GDPR erasure: pseudonymises the participant-profile display identity of wound-down (member-less)
    /// tenants — a projection copy of the erased sole trader's name/address.</summary>
    Task ScrubParticipantProfilesAsync(IReadOnlyList<Guid> tenantIds, CancellationToken ct = default);

    /// <summary>The subject's portable messages fragment (GDPR arts. 15/20): the message bodies they authored.</summary>
    Task<IReadOnlyList<MessageExport>> ExportMessagesAsync(Guid userId, CancellationToken ct = default);
}
