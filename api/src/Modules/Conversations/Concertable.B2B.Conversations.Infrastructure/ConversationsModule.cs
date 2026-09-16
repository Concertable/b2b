namespace Concertable.B2B.Conversations.Infrastructure;

internal sealed class ConversationsModule : IConversationsModule
{
    private readonly IMessageService messageService;

    public ConversationsModule(IMessageService messageService)
    {
        this.messageService = messageService;
    }

    public Task SendAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        MessageAction? action = null) =>
        messageService.SendAsync(participantTenantIds, senderTenantId, sentByUserId, content, action);

    public Task SendAndNotifyAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        MessageAction? action = null) =>
        messageService.SendAndNotifyAsync(participantTenantIds, senderTenantId, sentByUserId, content, action);
}
