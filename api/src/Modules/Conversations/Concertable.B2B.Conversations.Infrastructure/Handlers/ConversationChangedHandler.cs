using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Contracts.Events;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Tenant.Contracts;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Conversations.Infrastructure.Handlers;

internal sealed class ConversationChangedHandler : IIntegrationEventHandler<ConversationChanged>
{
    private readonly IConversationPrivilegedRepository repository;
    private readonly ITenantModule tenantModule;
    private readonly IConversationsNotifier notifier;
    private readonly TimeProvider timeProvider;

    public ConversationChangedHandler(
        IConversationPrivilegedRepository repository,
        ITenantModule tenantModule,
        IConversationsNotifier notifier,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.tenantModule = tenantModule;
        this.notifier = notifier;
        this.timeProvider = timeProvider;
    }

    public async Task HandleAsync(
        ConversationChanged e,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        var conversation = await repository.GetWithGrantsByIdAsync(e.ConversationId, ct);
        if (conversation is null)
            return;
        var tenantIds = conversation.AccessGrants
            .Where(grant => grant.Scope == ConversationAccessScope.Read && grant.RevokedAt is null)
            .Select(grant => grant.TenantId)
            .Distinct()
            .ToArray();
        var memberships = await tenantModule.GetCurrentMembershipsAsync(tenantIds, ct);
        var at = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var recipient in memberships
                     .Where(actor =>
                         actor.HasPermission(TenantPermission.MessagesRead)
                         && conversation.AccessGrants.AsQueryable().Any(
                             ResourceAccessGrant<ConversationAccessScope>.IsLiveFor<ConversationAccessGrant>(
                                 actor, TenantPermission.MessagesRead, at, ConversationAccessScope.Read)))
                     .DistinctBy(actor => actor.UserId))
            await notifier.ConversationChangedAsync(recipient.UserId, e.ConversationId, ct);
    }
}
