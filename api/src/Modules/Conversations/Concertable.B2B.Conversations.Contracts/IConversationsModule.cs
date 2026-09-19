namespace Concertable.B2B.Conversations.Contracts;

public interface IConversationsModule
{
    Task<int> CreateAsync(
        Guid requestId,
        IReadOnlyCollection<Guid> participantTenantIds,
        CancellationToken ct = default);

    Task SendAsync(
        int conversationId,
        Guid requestId,
        string content,
        MessageAction? action = null,
        CancellationToken ct = default);
}
