using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

/// <summary>
/// Provisions a tenant when a venue or artist manager registers — the one-tenant-per-operator rule (see
/// <c>TENANT_SCOPING_PLAN</c>). Idempotent: skips if this event was already consumed or a tenant already
/// exists for the user, so it is a no-op when the dev/test seeder has already created the tenant.
/// <see cref="TenantEntity.Create"/> raises the event that drives Payment provisioning, so both this path and
/// the seeder announce the tenant the same way.
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

        if (await context.Tenants.AnyAsync(t => t.CreatedByUserId == e.UserId, ct))
            return;

        context.AddInboxMessage(envelope, nameof(TenantProvisioningHandler));
        context.Tenants.Add(TenantEntity.Create(e.Email, e.UserId, timeProvider.GetUtcNow().UtcDateTime));
        await context.SaveChangesAsync(ct);
    }
}
