using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

/// <summary>
/// Provisions a tenant when a venue or artist manager registers — the one-tenant-per-operator rule (see
/// <c>TENANT_SCOPING_PLAN</c>). Idempotent per <see cref="CredentialRegisteredEvent"/> via the inbox. Creates the
/// tenant if absent; if the dev/E2E seeder already inserted it, re-announces it instead of skipping, so Payment
/// provisions off the resulting <c>TenantCreatedEvent</c> either way (Payment's own inbox dedups).
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

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.CreatedByUserId == e.UserId, ct);
        if (tenant is null)
            context.Tenants.Add(TenantEntity.Create(e.Email, e.UserId, timeProvider.GetUtcNow().UtcDateTime));
        else
            tenant.Announce();

        await context.SaveChangesAsync(ct);
    }
}
