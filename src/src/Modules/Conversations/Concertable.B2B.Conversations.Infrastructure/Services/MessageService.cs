using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Venue.Contracts;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class MessageService : IMessageService
{
    private const string UnknownOrg = "Unknown";

    private readonly IMessageRepository repository;
    private readonly IConversationsNotifier notifier;
    private readonly IBus bus;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;
    private readonly ICurrentUser currentUser;
    private readonly ITenantContext tenantContext;
    private readonly ITenantModule tenantModule;
    private readonly IUserModule userModule;
    private readonly IVenueModule venueModule;
    private readonly IArtistModule artistModule;
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
        IVenueModule venueModule,
        IArtistModule artistModule,
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
        this.venueModule = venueModule;
        this.artistModule = artistModule;
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
        var payload = message.ToDto(await ResolveOrgSenderAsync(senderTenantId, senderTenantId == venueTenantId), senderTenantId);

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
        var recipientPersona = recipientTenantId == venueTenantId ? "venue" : "artist";
        return new TenantActivityRecordedEvent(new ActivityRecord(
            $"message:{Guid.CreateVersion7(at)}",
            recipientTenantId,
            ToActivityType(action),
            at,
            content,
            null,
            $"/_{recipientPersona}/?inbox=open"));
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
            var sender = await ResolveOrgSenderAsync(preview.CounterpartTenantId, preview.CounterpartIsVenue);
            var persona = preview.CounterpartIsVenue ? "artist" : "venue";
            responses.Add(new MessagePreviewDto(
                preview.Id,
                sender.DisplayName,
                null,
                preview.Preview,
                preview.At,
                preview.Unread,
                $"/_{persona}/?inbox=open"));
        }

        return responses;
    }

    public Task MarkInboxReadAsync() =>
        repository.AdvanceReadPointersAsync(tenantContext.GetTenantId(), currentUser.GetId(), timeProvider.GetUtcNow().DateTime);

    private async Task<Pagination<MessageDto>> ToPaginationAsync(IPagination<MessageEntity> messages)
    {
        var activeTenantId = tenantContext.GetTenantId();
        var senders = await ResolveSendersAsync(messages.Data, activeTenantId);
        return new Pagination<MessageDto>(
            messages.Data.Select(m => m.ToDto(senders[m.Id], CounterpartOf(m, activeTenantId))).ToList(),
            messages.TotalCount,
            messages.PageNumber,
            messages.PageSize);
    }

    private static Guid CounterpartOf(MessageEntity message, Guid activeTenantId) =>
        activeTenantId == message.VenueTenantId ? message.ArtistTenantId : message.VenueTenantId;

    private async Task<Dictionary<int, MessageSender>> ResolveSendersAsync(IReadOnlyList<MessageEntity> messages, Guid activeTenantId)
    {
        var emails = await ResolveMemberEmailsAsync(messages, activeTenantId);
        var orgs = await ResolveCounterpartyOrgsAsync(messages, activeTenantId);

        return messages.ToDictionary(
            m => m.Id,
            m => m.SenderTenantId == activeTenantId
                ? MessageSender.Member(emails.GetValueOrDefault(m.SentByUserId, UnknownOrg))
                : orgs[m.SenderTenantId]);
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

    private async Task<Dictionary<Guid, MessageSender>> ResolveCounterpartyOrgsAsync(IReadOnlyList<MessageEntity> messages, Guid activeTenantId)
    {
        var counterparties = messages
            .Where(m => m.SenderTenantId != activeTenantId)
            .Select(m => (TenantId: m.SenderTenantId, IsVenue: m.SenderTenantId == m.VenueTenantId))
            .Distinct()
            .ToList();

        var orgs = new Dictionary<Guid, MessageSender>();
        foreach (var (tenantId, isVenue) in counterparties)
            orgs[tenantId] = await ResolveOrgSenderAsync(tenantId, isVenue);

        return orgs;
    }

    private async Task<MessageSender> ResolveOrgSenderAsync(Guid tenantId, bool isVenue)
    {
        if (isVenue)
        {
            var venue = await venueModule.GetOrgIdentityByTenantIdAsync(tenantId);
            if (venue is not null)
                return MessageSender.Org(venue.Name, venue.County, venue.Town);
        }
        else
        {
            var artist = await artistModule.GetOrgIdentityByTenantIdAsync(tenantId);
            if (artist is not null)
                return MessageSender.Org(artist.Name, artist.County, artist.Town);
        }

        var tenant = await tenantModule.GetByIdAsync(tenantId);
        return MessageSender.Org(tenant?.LegalName ?? UnknownOrg, null, null);
    }
}
