namespace Concertable.B2B.Conversations.Infrastructure;

internal sealed class ConversationsModule : IConversationsModule
{
    private readonly IMessageService messageService;
    private readonly IConversationsErasureService erasureService;

    public ConversationsModule(IMessageService messageService, IConversationsErasureService erasureService)
    {
        this.messageService = messageService;
        this.erasureService = erasureService;
    }

    public Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null) =>
        messageService.SendAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);

    public Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null) =>
        messageService.SendAndNotifyAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);

    public Task SeverAuthoredMessagesAsync(Guid userId, CancellationToken ct = default) =>
        erasureService.SeverAuthoredMessagesAsync(userId, ct);

    public Task ScrubParticipantProfilesAsync(IReadOnlyList<Guid> tenantIds, CancellationToken ct = default) =>
        erasureService.ScrubParticipantProfilesAsync(tenantIds, ct);

    public Task<IReadOnlyList<MessageExport>> ExportMessagesAsync(Guid userId, CancellationToken ct = default) =>
        erasureService.ExportMessagesAsync(userId, ct);
}
