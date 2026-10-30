namespace Concertable.B2B.Conversations.Infrastructure;

internal sealed class ConversationsModule : IConversationsModule
{
    private readonly IMessageService messageService;
    private readonly ISubjectMessageReader subjectMessageReader;

    public ConversationsModule(IMessageService messageService, ISubjectMessageReader subjectMessageReader)
    {
        this.messageService = messageService;
        this.subjectMessageReader = subjectMessageReader;
    }

    public Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null) =>
        messageService.SendAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);

    public Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null) =>
        messageService.SendAndNotifyAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);

    public Task<IReadOnlyList<SubjectMessageDto>> GetSubjectMessagesAsync(Guid userId, CancellationToken ct = default) =>
        this.subjectMessageReader.GetSubjectMessagesAsync(userId, ct);
}
