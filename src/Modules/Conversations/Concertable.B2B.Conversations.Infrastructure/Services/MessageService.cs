using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class MessageService : IMessageService
{
    private readonly IMessageRepository repository;
    private readonly IConversationsNotifier notifier;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly TimeProvider timeProvider;

    public MessageService(
        IMessageRepository repository,
        IConversationsNotifier notifier,
        ICurrentUser currentUser,
        IUserModule userModule,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.notifier = notifier;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
    }

    public async Task SendAsync(Guid fromUserId, Guid toUserId, string content, MessageAction? action = null)
    {
        var message = MessageEntity.Create(fromUserId, toUserId, content, timeProvider.GetUtcNow().DateTime, action);
        await repository.AddAsync(message);
        await repository.SaveChangesAsync();
    }

    public async Task SendAndNotifyAsync(Guid fromUserId, Guid toUserId, string content, MessageAction? action = null)
    {
        var message = MessageEntity.Create(fromUserId, toUserId, content, timeProvider.GetUtcNow().DateTime, action);

        await repository.AddAsync(message);
        await repository.SaveChangesAsync();

        var fromUser = await GetSenderDtoAsync(fromUserId);
        await notifier.MessageReceivedAsync(toUserId.ToString(), message.ToDto(fromUser));
    }

    public async Task<IPagination<MessageDto>> GetForUserAsync(IPageParams pageParams)
    {
        var userId = currentUser.GetId();
        var messages = await repository.GetByUserIdAsync(userId, pageParams);
        var senders = await GetSenderDtosAsync(messages.Data);

        return new Pagination<MessageDto>(
            messages.Data.Select(m => m.ToDto(senders[m.FromUserId])).ToList(),
            messages.TotalCount,
            messages.PageNumber,
            messages.PageSize);
    }

    public async Task<MessageSummary> GetSummaryForUser()
    {
        var pageParams = new PageParams { PageNumber = 1, PageSize = 5 };

        var userId = currentUser.GetId();
        var messages = await repository.GetByUserIdAsync(userId, pageParams);
        var unreadCount = await repository.GetUnreadCountByUserIdAsync(userId);
        var senders = await GetSenderDtosAsync(messages.Data);

        var pagination = new Pagination<MessageDto>(
            messages.Data.Select(m => m.ToDto(senders[m.FromUserId])).ToList(),
            messages.TotalCount,
            messages.PageNumber,
            messages.PageSize);

        return new MessageSummary(pagination, unreadCount);
    }

    public Task<int> GetUnreadCountForUserAsync() =>
        repository.GetUnreadCountByUserIdAsync(currentUser.GetId());

    public Task MarkAsReadAsync(List<int> ids) =>
        repository.MarkAsReadAsync(ids);

    private async Task<MessageUser> GetSenderDtoAsync(Guid fromUserId)
    {
        var sender = await userModule.GetByIdAsync(fromUserId)
            ?? throw new NotFoundException("Message sender not found");
        return sender.ToMessageUser();
    }

    private async Task<Dictionary<Guid, MessageUser>> GetSenderDtosAsync(IEnumerable<MessageEntity> messages)
    {
        var senderIds = messages.Select(m => m.FromUserId).Distinct().ToList();
        var senders = await userModule.GetByIdsAsync(senderIds);
        return senders.ToDictionary(s => s.Id, s => s.ToMessageUser());
    }
}
