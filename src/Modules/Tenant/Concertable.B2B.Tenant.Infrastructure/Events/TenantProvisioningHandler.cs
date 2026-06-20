using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

/// <summary>
/// Provisions a tenant when a venue or artist manager registers — the one-tenant-per-operator rule — and its
/// founding Owner membership, the source of truth for who may act in the tenant. Idempotent per
/// <see cref="CredentialRegisteredEvent"/> via the inbox. This is the single, reliable <c>TenantCreatedEvent</c>
/// trigger: it fires after the ASB subscriptions exist (registration events arrive once the listener is up).
/// Creates the tenant if absent (<c>Create</c> raises the event); a dev/E2E-seeded tenant is already present with
/// its create event suppressed at seed time, so this re-raises it via <c>Announce()</c>. Exactly one publish per
/// tenant either way — no duplicate, orphaned Stripe accounts. The Owner membership is ensured idempotently over
/// the seeded row, so it exists exactly once regardless of seed/handler ordering.
/// </summary>
internal sealed class TenantProvisioningHandler : IIntegrationEventHandler<CredentialRegisteredEvent>
{
    private static readonly IReadOnlyDictionary<string, TenantType> PersonaByClientId = new Dictionary<string, TenantType>
    {
        [ClientIds.VenueWeb] = TenantType.Venue,
        [ClientIds.VenueMobile] = TenantType.Venue,
        [ClientIds.ArtistWeb] = TenantType.Artist,
        [ClientIds.ArtistMobile] = TenantType.Artist,
    };

    private readonly TenantDbContext context;
    private readonly TimeProvider timeProvider;

    public TenantProvisioningHandler(TenantDbContext context, TimeProvider timeProvider)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task HandleAsync(CredentialRegisteredEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (!PersonaByClientId.TryGetValue(e.ClientId, out var type))
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(TenantProvisioningHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(TenantProvisioningHandler));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.CreatedByUserId == e.UserId, ct);
        if (tenant is null)
        {
            tenant = TenantEntity.Create(e.Email, e.UserId, type, now);
            context.Tenants.Add(tenant);
        }
        else
        {
            tenant.Announce();
        }

        var hasOwnerMembership = await context.Memberships
            .AnyAsync(m => m.TenantId == tenant.Id && m.UserId == e.UserId, ct);
        if (!hasOwnerMembership)
            context.Memberships.Add(
                TenantMembershipEntity.Create(tenant.Id, e.UserId, TenantRole.Owner, invitedBy: null, now));

        await context.SaveChangesAsync(ct);
    }
}
