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
    private readonly IPermissionCatalog permissionCatalog;
    private readonly IConversationsNotifier notifier;
    private readonly TimeProvider timeProvider;

    public ConversationChangedHandler(
        IConversationPrivilegedRepository repository,
        ITenantModule tenantModule,
        IPermissionCatalog permissionCatalog,
        IConversationsNotifier notifier,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.tenantModule = tenantModule;
        this.permissionCatalog = permissionCatalog;
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
                         permissionCatalog.Grants(actor.Role, TenantPermission.MessagesRead)
                         && ResourceGrantPolicy.Allows(
                             conversation.AccessGrants,
                             ConversationAccessScope.Read,
                             actor,
                             permissionCatalog.AudienceFor(actor.Role, TenantPermission.MessagesRead),
                             at))
                     .DistinctBy(actor => actor.UserId))
            await notifier.ConversationChangedAsync(recipient.UserId, e.ConversationId, ct);
    }
}
