using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

/// <summary>
/// Provisions a tenant when a venue or artist manager registers — the one-tenant-per-operator rule (see
/// <c>TENANT_SCOPING_PLAN</c>). Idempotent per <see cref="CredentialRegisteredEvent"/> via the inbox. Creates the
/// tenant only if absent: <c>TenantEntity.Create</c> raises <c>TenantCreatedEvent</c> so Payment provisions. A
/// dev/E2E-seeded tenant is already present (and already published its own create event on insert), so this
/// no-ops on it — re-publishing would make Payment provision a second, orphaned Stripe account.
/// </summary>
internal sealed class TenantProvisioningHandler : IIntegrationEventHandler<CredentialRegisteredEvent>
{
    private static readonly HashSet<string> ManagerClientIds =
        [ClientIds.VenueWeb, ClientIds.VenueMobile, ClientIds.ArtistWeb, ClientIds.ArtistMobile];

    private readonly TenantDbContext context;
    private readonly TimeProvider timeProvider;

    public TenantProvisioningHandler(TenantDbContext context, TimeProvider timeProvider)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task HandleAsync(CredentialRegisteredEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (!ManagerClientIds.Contains(e.ClientId))
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(TenantProvisioningHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(TenantProvisioningHandler));

        var exists = await context.Tenants.AnyAsync(t => t.CreatedByUserId == e.UserId, ct);
        if (!exists)
            context.Tenants.Add(TenantEntity.Create(e.Email, e.UserId, timeProvider.GetUtcNow().UtcDateTime));

        await context.SaveChangesAsync(ct);
    }
}
