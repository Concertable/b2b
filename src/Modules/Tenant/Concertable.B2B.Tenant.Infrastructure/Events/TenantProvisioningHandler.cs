using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

/// <summary>
/// Provisions a tenant when a venue or artist manager registers — the one-tenant-per-operator rule (see
/// <c>TENANT_SCOPING_PLAN</c>). Idempotent per <see cref="CredentialRegisteredEvent"/> via the inbox. This is the
/// single, reliable <c>TenantCreatedEvent</c> trigger: it fires after the ASB subscriptions exist (registration
/// events arrive once the listener is up). Creates the tenant if absent (<c>Create</c> raises the event); a
/// dev/E2E-seeded tenant is already present with its create event suppressed at seed time, so this re-raises it
/// via <c>Announce()</c>. Exactly one publish per tenant either way — no duplicate, orphaned Stripe accounts.
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
