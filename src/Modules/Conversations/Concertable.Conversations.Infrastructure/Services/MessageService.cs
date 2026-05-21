using Concertable.Shared.Exceptions;

namespace Concertable.Conversations.Infrastructure.Services;

internal class MessageService : IMessageService
{
    private readonly IMessageRepository messageRepository;
    private readonly IConversationsNotifier notifier;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly TimeProvider timeProvider;

    public MessageService(
        IMessageRepository messageRepository,
        IConversationsNotifier notifier,
        ICurrentUser currentUser,
        IUserModule userModule,
        TimeProvider timeProvider)
    {
        this.messageRepository = messageRepository;
        this.notifier = notifier;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
    }

    public async Task SendAsync(Guid fromUserId, Guid toUserId, string content, MessageAction? action = null)
    {
        var message = MessageEntity.Create(fromUserId, toUserId, content, timeProvider.GetUtcNow().DateTime, action);
        await messageRepository.AddAsync(message);
        await messageRepository.SaveChangesAsync();
    }

    public async Task SendAndNotifyAsync(Guid fromUserId, Guid toUserId, string content, MessageAction? action = null)
    {
        var message = MessageEntity.Create(fromUserId, toUserId, content, timeProvider.GetUtcNow().DateTime, action);

        await messageRepository.AddAsync(message);
        await messageRepository.SaveChangesAsync();

        var fromUser = await GetSenderDtoAsync(fromUserId);
        await notifier.MessageReceivedAsync(toUserId.ToString(), message.ToDto(fromUser));
    }

    public async Task<IPagination<MessageDto>> GetForUserAsync(IPageParams pageParams)
    {
        var userId = currentUser.GetId();
        var messages = await messageRepository.GetByUserIdAsync(userId, pageParams);
        var senders = await GetSenderDtosAsync(messages.Data);

        return new Pagination<MessageDto>(
            messages.Data.Select(m => m.ToDto(senders[m.FromUserId])).ToList(),
            messages.TotalCount,
            messages.PageNumber,
            messages.PageSize);
    }

    public async Task<MessageSummaryDto> GetSummaryForUser()
    {
        var pageParams = new PageParams { PageNumber = 1, PageSize = 5 };

        var userId = currentUser.GetId();
        var messages = await messageRepository.GetByUserIdAsync(userId, pageParams);
        var unreadCount = await messageRepository.GetUnreadCountByUserIdAsync(userId);
        var senders = await GetSenderDtosAsync(messages.Data);

        var pagination = new Pagination<MessageDto>(
            messages.Data.Select(m => m.ToDto(senders[m.FromUserId])).ToList(),
            messages.TotalCount,
            messages.PageNumber,
            messages.PageSize);

        return new MessageSummaryDto(pagination, unreadCount);
    }

    public Task<int> GetUnreadCountForUserAsync() =>
        messageRepository.GetUnreadCountByUserIdAsync(currentUser.GetId());

    public Task MarkAsReadAsync(List<int> ids) =>
        messageRepository.MarkAsReadAsync(ids);

    private async Task<MessageUserDto> GetSenderDtoAsync(Guid fromUserId)
    {
        var sender = await userModule.GetByIdAsync(fromUserId)
            ?? throw new NotFoundException("Message sender not found");
        return sender.ToMessageUserDto();
    }

    private async Task<Dictionary<Guid, MessageUserDto>> GetSenderDtosAsync(IEnumerable<MessageEntity> messages)
    {
        var dict = new Dictionary<Guid, MessageUserDto>();
        foreach (var fromUserId in messages.Select(m => m.FromUserId).Distinct())
            dict[fromUserId] = await GetSenderDtoAsync(fromUserId);
        return dict;
    }
}
