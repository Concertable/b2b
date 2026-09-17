namespace Concertable.B2B.Conversations.Infrastructure;

internal sealed class ConversationsModule : IConversationsModule
{
    private readonly IMessageService messageService;
    private readonly IConversationsErasureService erasureService;
    private readonly ISubjectMessageReader subjectMessageReader;

    public ConversationsModule(IMessageService messageService, IConversationsErasureService erasureService, ISubjectMessageReader subjectMessageReader)
    {
        this.messageService = messageService;
        this.erasureService = erasureService;
        this.subjectMessageReader = subjectMessageReader;
    }

    public Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null) =>
        messageService.SendAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);

    public Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null) =>
        messageService.SendAndNotifyAsync(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, action);

    public Task SeverAuthoredMessagesAsync(Guid userId, CancellationToken ct = default) =>
        erasureService.SeverAuthoredMessagesAsync(userId, ct);

    public Task ScrubParticipantProfilesAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default) =>
        erasureService.ScrubParticipantProfilesAsync(tenantIds, ct);

    public Task<IReadOnlyList<SubjectMessageDto>> GetSubjectMessagesAsync(Guid userId, CancellationToken ct = default) =>
        subjectMessageReader.GetSubjectMessagesAsync(userId, ct);
}
