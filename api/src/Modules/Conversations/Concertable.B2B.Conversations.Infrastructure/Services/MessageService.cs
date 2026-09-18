using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class MessageService : IMessageService
{
    private const string UnknownSender = "Unknown";

    /* The neutral business surface hosts the inbox. It used to be one of two persona apps chosen from which
       side of the pair the recipient was on, which a business that is neither cannot answer. */
    private const string InboxHref = "/?inbox=open";

    private readonly IMessageRepository repository;
    private readonly IThreadRepository threadRepository;
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
        IThreadRepository threadRepository,
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
        this.threadRepository = threadRepository;
        this.notifier = notifier;
        this.bus = bus;
        this.outboxBehavior = outboxBehavior;
        this.currentUser = currentUser;
        this.tenantContext = tenantContext;
        this.tenantModule = tenantModule;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
    }

    public async Task SendAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        MessageAction? action = null) =>
        await SendCoreAsync(participantTenantIds, senderTenantId, sentByUserId, content, action);

    public async Task SendAndNotifyAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        MessageAction? action = null)
    {
        var message = await SendCoreAsync(participantTenantIds, senderTenantId, sentByUserId, content, action);
        var payload = message.ToDto(await ResolveParticipantAsync(senderTenantId), senderTenantId);

        foreach (var recipientTenantId in await RecipientsOfAsync(message.ThreadId, senderTenantId))
        {
            foreach (var memberId in await tenantModule.GetMemberUserIdsAsync(recipientTenantId))
                await notifier.MessageReceivedAsync(memberId.ToString(), payload);
        }
    }

    private async Task<MessageEntity> SendCoreAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        MessageAction? action)
    {
        var at = timeProvider.GetUtcNow();
        var existing = await threadRepository.GetByParticipantsAsync(participantTenantIds);
        MessageEntity? message = null;

        await outboxBehavior.ExecuteAsync(async () =>
        {
            var thread = existing;
            if (thread is null)
            {
                // Saved before the message so the generated thread id, and its grants, are real.
                thread = ThreadEntity.Create(participantTenantIds, at.UtcDateTime);
                await threadRepository.InsertAsync(thread);
            }

            message = MessageEntity.Create(thread.Id, senderTenantId, sentByUserId, content, at.UtcDateTime, action);
            await repository.AddAsync(message);

            foreach (var recipientTenantId in participantTenantIds.Where(id => id != senderTenantId))
                await bus.PublishAsync(CreateActivityEvent(recipientTenantId, content, action, at));
        });

        return message ?? throw new InvalidOperationException("The send completed without producing a message.");
    }

    private async Task<IReadOnlyList<Guid>> RecipientsOfAsync(int threadId, Guid senderTenantId) =>
        [.. (await threadRepository.GetParticipantTenantIdsAsync(threadId)).Where(id => id != senderTenantId)];

    private static TenantActivityRecordedEvent CreateActivityEvent(
        Guid recipientTenantId,
        string content,
        MessageAction? action,
        DateTimeOffset at) =>
        new(new ActivityRecord(
            $"message:{Guid.CreateVersion7(at)}",
            recipientTenantId,
            ToActivityType(action),
            at,
            content,
            null,
            InboxHref));

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
            var sender = preview.CounterpartTenantId is { } counterpartTenantId
                ? await ResolveParticipantAsync(counterpartTenantId)
                : MissingParticipant();
            responses.Add(new MessagePreviewDto(
                preview.Id,
                sender.DisplayName,
                preview.Preview,
                preview.At,
                preview.Unread,
                InboxHref));
        }

        return responses;
    }

    public Task MarkInboxReadAsync() =>
        repository.AdvanceReadPointersAsync(tenantContext.GetTenantId(), currentUser.GetId(), timeProvider.GetUtcNow().DateTime);

    private async Task<IPagination<MessageDto>> ToPaginationAsync(IPagination<MessageEntity> messages)
    {
        var activeTenantId = tenantContext.GetTenantId();
        var senders = await ResolveSendersAsync(messages.Data, activeTenantId);
        var counterparts = await ResolveCounterpartsAsync(messages.Data, activeTenantId);
        return messages.Map(m => m.ToDto(senders[m.Id], counterparts.GetValueOrDefault(m.ThreadId)));
    }

    private async Task<Dictionary<int, Guid>> ResolveCounterpartsAsync(
        IReadOnlyList<MessageEntity> messages,
        Guid activeTenantId)
    {
        var counterparts = new Dictionary<int, Guid>();
        foreach (var threadId in messages.Select(m => m.ThreadId).Distinct())
        {
            var others = (await threadRepository.GetParticipantTenantIdsAsync(threadId))
                .Where(id => id != activeTenantId)
                .ToList();
            if (others is [var sole, ..])
                counterparts[threadId] = sole;
        }

        return counterparts;
    }

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
