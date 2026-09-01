using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class MessageService : IMessageService
{
    private const string UnknownSender = "Unknown";

    private readonly IMessageRepository repository;
    private readonly IConversationsNotifier notifier;
    private readonly IBus bus;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;
    private readonly ICurrentUser currentUser;
    private readonly ITenantContext tenantContext;
    private readonly ITenantModule tenantModule;
    private readonly IUserModule userModule;
    private readonly TimeProvider timeProvider;

    public MessageService(
        IMessageRepository repository,
        IConversationsNotifier notifier,
        IBus bus,
        IOutboxUnitOfWorkBehavior outboxBehavior,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        ITenantModule tenantModule,
        IUserModule userModule,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.notifier = notifier;
        this.bus = bus;
        this.outboxBehavior = outboxBehavior;
        this.currentUser = currentUser;
        this.tenantContext = tenantContext;
        this.tenantModule = tenantModule;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
    }

    public async Task SendAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null)
    {
        var at = timeProvider.GetUtcNow();
        var message = MessageEntity.Create(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, at.UtcDateTime, action);
        await outboxBehavior.ExecuteAsync(async () =>
        {
            await repository.AddAsync(message);
            await bus.PublishAsync(CreateActivityEvent(venueTenantId, artistTenantId, senderTenantId, content, action, at));
        });
    }

    public async Task SendAndNotifyAsync(Guid venueTenantId, Guid artistTenantId, Guid senderTenantId, Guid sentByUserId, string content, MessageAction? action = null)
    {
        var at = timeProvider.GetUtcNow();
        var message = MessageEntity.Create(venueTenantId, artistTenantId, senderTenantId, sentByUserId, content, at.UtcDateTime, action);
        await outboxBehavior.ExecuteAsync(async () =>
        {
            await repository.AddAsync(message);
            await bus.PublishAsync(CreateActivityEvent(venueTenantId, artistTenantId, senderTenantId, content, action, at));
        });

        var recipientTenantId = senderTenantId == venueTenantId ? artistTenantId : venueTenantId;
        var payload = message.ToDto(await ResolveParticipantAsync(senderTenantId), senderTenantId);

        foreach (var memberId in await tenantModule.GetMemberUserIdsAsync(recipientTenantId))
            await notifier.MessageReceivedAsync(memberId.ToString(), payload);
    }

    private static TenantActivityRecordedEvent CreateActivityEvent(
        Guid venueTenantId,
        Guid artistTenantId,
        Guid senderTenantId,
        string content,
        MessageAction? action,
        DateTimeOffset at)
    {
        var recipientTenantId = senderTenantId == venueTenantId ? artistTenantId : venueTenantId;
        var recipientSurface = recipientTenantId == venueTenantId ? "venue" : "artist";
        return new TenantActivityRecordedEvent(new ActivityRecord(
            $"message:{Guid.CreateVersion7(at)}",
            recipientTenantId,
            ToActivityType(action),
            at,
            content,
            null,
            $"/_{recipientSurface}/?inbox=open"));
    }

    private static ActivityType ToActivityType(MessageAction? action) =>
        action switch
        {
            MessageAction.ApplicationReceived => ActivityType.ApplicationReceived,
            MessageAction.ApplicationAccepted => ActivityType.ApplicationAccepted,
            MessageAction.ApplicationRejected => ActivityType.ApplicationDeclined,
            MessageAction.ApplicationWithdrawn => ActivityType.ApplicationWithdrawn,
            MessageAction.ApplicationCancelled => ActivityType.ApplicationCancelled,
            _ => ActivityType.MessageReceived
        };

    public async Task<IPagination<MessageDto>> GetInboxAsync(IPageParams pageParams)
    {
        var messages = await repository.GetByTenantIdAsync(tenantContext.GetTenantId(), pageParams);
        return await ToPaginationAsync(messages);
    }

    public Task<int> GetUnreadCountForUserAsync() =>
        repository.GetUnreadCountByTenantIdAsync(tenantContext.GetTenantId(), currentUser.GetId());

    public async Task<IReadOnlyList<MessagePreviewDto>> GetRecentPreviewsAsync()
    {
        var activeTenantId = tenantContext.GetTenantId();
        var previews = await repository.GetRecentPreviewsAsync(activeTenantId, currentUser.GetId());
        var responses = new List<MessagePreviewDto>(previews.Count);

        foreach (var preview in previews)
        {
            var sender = await ResolveParticipantAsync(preview.CounterpartTenantId);
            var persona = preview.CounterpartIsVenue ? "artist" : "venue";
            responses.Add(new MessagePreviewDto(
                preview.Id,
                sender.DisplayName,
                preview.Preview,
                preview.At,
                preview.Unread,
                $"/_{persona}/?inbox=open"));
        }

        return responses;
    }

    public Task MarkInboxReadAsync() =>
        repository.AdvanceReadPointersAsync(tenantContext.GetTenantId(), currentUser.GetId(), timeProvider.GetUtcNow().DateTime);

    private async Task<IPagination<MessageDto>> ToPaginationAsync(IPagination<MessageEntity> messages)
    {
        var activeTenantId = tenantContext.GetTenantId();
        var senders = await ResolveSendersAsync(messages.Data, activeTenantId);
        return messages.Map(m => m.ToDto(senders[m.Id], CounterpartOf(m, activeTenantId)));
    }

    private static Guid CounterpartOf(MessageEntity message, Guid activeTenantId) =>
        activeTenantId == message.VenueTenantId ? message.ArtistTenantId : message.VenueTenantId;

    private async Task<Dictionary<int, MessageSender>> ResolveSendersAsync(IReadOnlyList<MessageEntity> messages, Guid activeTenantId)
    {
        var emails = await ResolveMemberEmailsAsync(messages, activeTenantId);
        var profiles = await ResolveCounterpartyProfilesAsync(messages, activeTenantId);

        return messages.ToDictionary(
            m => m.Id,
            m => m.SenderTenantId == activeTenantId
                ? MessageSender.Member(emails.GetValueOrDefault(m.SentByUserId, UnknownSender))
                : profiles[m.SenderTenantId]);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveMemberEmailsAsync(IReadOnlyList<MessageEntity> messages, Guid activeTenantId)
    {
        var memberIds = messages
            .Where(m => m.SenderTenantId == activeTenantId)
            .Select(m => m.SentByUserId)
            .Distinct()
            .ToList();

        if (memberIds.Count == 0)
            return new Dictionary<Guid, string>();

        var members = await userModule.GetByIdsAsync(memberIds);
        return members.ToDictionary(u => u.Id, u => u.Email);
    }

    private async Task<Dictionary<Guid, MessageSender>> ResolveCounterpartyProfilesAsync(IReadOnlyList<MessageEntity> messages, Guid activeTenantId)
    {
        var tenantIds = messages
            .Where(m => m.SenderTenantId != activeTenantId)
            .Select(m => m.SenderTenantId)
            .ToHashSet();

        var profiles = await repository.GetParticipantProfilesAsync(tenantIds);
        var senders = profiles.ToDictionary(
            pair => pair.Key,
            pair => MessageSender.Org(pair.Value.Name, pair.Value.Address.County, pair.Value.Address.Town));

        foreach (var tenantId in tenantIds.Where(id => !profiles.ContainsKey(id)))
            senders[tenantId] = MissingParticipant();

        return senders;
    }

    private async Task<MessageSender> ResolveParticipantAsync(Guid tenantId)
    {
        var profiles = await repository.GetParticipantProfilesAsync(new HashSet<Guid> { tenantId });
        if (profiles.TryGetValue(tenantId, out var profile))
            return MessageSender.Org(profile.Name, profile.Address.County, profile.Address.Town);

        return MissingParticipant();
    }

    private static MessageSender MissingParticipant() => MessageSender.Org(UnknownSender, null, null);
}
