namespace Concertable.B2B.Conversations.Contracts;

public interface IConversationsModule
{
    Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null);
    Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null);

    Task SeverAuthoredMessagesAsync(Guid userId, CancellationToken ct = default);

    Task ScrubParticipantProfilesAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);

    Task<IReadOnlyList<SubjectMessageDto>> GetSubjectMessagesAsync(Guid userId, CancellationToken ct = default);
}
