using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Contracts.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Conversations.Infrastructure.Handlers;

internal sealed class ConversationChangedHandler : IIntegrationEventHandler<ConversationChanged>
{
    private readonly IConversationPrivilegedRepository repository;
    private readonly ITenantModule tenantModule;
    private readonly IConversationsNotifier notifier;

    public ConversationChangedHandler(
        IConversationPrivilegedRepository repository,
        ITenantModule tenantModule,
        IConversationsNotifier notifier)
    {
        this.repository = repository;
        this.tenantModule = tenantModule;
        this.notifier = notifier;
    }

    public async Task HandleAsync(
        ConversationChanged e,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        var conversation = await repository.GetByIdAsync(e.ConversationId, ct);
        if (conversation is null)
            return;
        var tenantIds = conversation.Participants.Select(participant => participant.TenantId).ToArray();
        var memberships = await tenantModule.GetCurrentMembershipsAsync(tenantIds, ct);
        foreach (var recipient in memberships
                     .Where(conversation.IsReadableBy)
                     .DistinctBy(actor => actor.UserId))
            await notifier.ConversationChangedAsync(recipient.UserId, e.ConversationId, ct);
    }
}
