using Concertable.B2B.Tenant.Contracts;
using Concertable.Contracts;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class MessageService : IMessageService
{
    private const string UnknownSender = "Unknown";

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
        var payload = message.ToDto(await ResolveParticipantAsync(senderTenantId), senderTenantId);

        foreach (var memberId in await tenantModule.GetMemberUserIdsAsync(recipientTenantId))
            await notifier.MessageReceivedAsync(memberId.ToString(), payload);
    }

    public async Task<IPagination<MessageDto>> GetInboxAsync(IPageParams pageParams)
    {
        var messages = await repository.GetByTenantIdAsync(tenantContext.GetTenantId(), pageParams);
        return await ToPaginationAsync(messages);
    }

    public Task<int> GetUnreadCountForUserAsync() =>
        repository.GetUnreadCountByTenantIdAsync(tenantContext.GetTenantId(), currentUser.GetId());

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
