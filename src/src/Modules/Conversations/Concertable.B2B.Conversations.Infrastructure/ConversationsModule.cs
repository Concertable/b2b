namespace Concertable.B2B.Conversations.Infrastructure;

internal sealed class ConversationsModule : IConversationsModule
{
    private readonly IMessageService messageService;

    public ConversationsModule(IMessageService messageService)
    {
        this.messageService = messageService;
    }

    public Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null) =>
        messageService.SendAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);

    public Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null) =>
        messageService.SendAndNotifyAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);
}
