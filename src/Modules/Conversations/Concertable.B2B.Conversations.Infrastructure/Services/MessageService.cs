using Concertable.B2B.Conversations;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class MessageService : IMessageService
{
    private readonly IMessageRepository repository;
    private readonly IConversationsNotifier notifier;
    private readonly ICurrentUser currentUser;
    private readonly ITenantContext tenantContext;
    private readonly ITenantModule tenantModule;
    private readonly IUserModule userModule;
    private readonly TimeProvider timeProvider;

    public MessageService(
        IMessageRepository repository,
        IConversationsNotifier notifier,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        ITenantModule tenantModule,
        IUserModule userModule,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.notifier = notifier;
        this.currentUser = currentUser;
        this.tenantContext = tenantContext;
        this.tenantModule = tenantModule;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
    }

    public async Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null)
    {
        var message = MessageEntity.Create(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, timeProvider.GetUtcNow().DateTime, action);
        await repository.AddAsync(message);
        await repository.SaveChangesAsync();
    }

    public async Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null)
    {
        var message = MessageEntity.Create(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, timeProvider.GetUtcNow().DateTime, action);
        await repository.AddAsync(message);
        await repository.SaveChangesAsync();

        var recipientTenantId = senderTenantId == venueTenantId ? artistTenantId : venueTenantId;
        var payload = message.ToDto(await GetSenderDtoAsync(sentByUserId));

        foreach (var memberId in await tenantModule.GetMemberUserIdsAsync(recipientTenantId))
            await notifier.MessageReceivedAsync(memberId.ToString(), payload);
    }

    public async Task<IPagination<MessageDto>> GetInboxAsync(IPageParams pageParams)
    {
        var messages = await repository.GetByTenantIdAsync(tenantContext.GetTenantId(), pageParams);
        return await ToPaginationAsync(messages);
    }

    public async Task<MessageSummary> GetInboxSummaryAsync()
    {
        var activeTenantId = tenantContext.GetTenantId();
        var messages = await repository.GetByTenantIdAsync(activeTenantId, new PageParams { PageNumber = 1, PageSize = 5 });
        var unreadCount = await repository.GetUnreadCountByTenantIdAsync(activeTenantId, currentUser.GetId());
        return new MessageSummary(await ToPaginationAsync(messages), unreadCount);
    }

    public Task<int> GetUnreadCountForUserAsync() =>
        repository.GetUnreadCountByTenantIdAsync(tenantContext.GetTenantId(), currentUser.GetId());

    public Task MarkAsReadAsync(Guid counterpartTenantId) =>
        repository.AdvanceReadPointerAsync(tenantContext.GetTenantId(), counterpartTenantId, currentUser.GetId(), timeProvider.GetUtcNow().DateTime);

    private async Task<Pagination<MessageDto>> ToPaginationAsync(IPagination<MessageEntity> messages)
    {
        var senders = await GetSenderDtosAsync(messages.Data);
        return new Pagination<MessageDto>(
            messages.Data.Select(m => m.ToDto(senders[m.SentByUserId])).ToList(),
            messages.TotalCount,
            messages.PageNumber,
            messages.PageSize);
    }

    private async Task<MessageUser> GetSenderDtoAsync(Guid sentByUserId)
    {
        var sender = await userModule.GetByIdAsync(sentByUserId)
            .OrNotFound(DisplayNames.MessageSender);
        return sender.ToMessageUser();
    }

    private async Task<Dictionary<Guid, MessageUser>> GetSenderDtosAsync(IEnumerable<MessageEntity> messages)
    {
        var senderIds = messages.Select(m => m.SentByUserId).Distinct().ToList();
        var senders = await userModule.GetByIdsAsync(senderIds);
        return senders.ToDictionary(s => s.Id, s => s.ToMessageUser());
    }
}
