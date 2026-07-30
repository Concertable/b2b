using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

/// <summary>
/// Publishes the integration <see cref="PayoutOwnerRegisteredEvent"/> (B2B's wire-compatible mirror of
/// Payment's own event) when a tenant is created — fires for both the production registration path and
/// the dev/E2E seeder (both go through <c>TenantEntity.Create</c>), so Payment provisions a Stripe account
/// keyed on the tenant id without depending on B2B's contracts. Pre-commit so the outbox row commits with
/// the tenant.
/// </summary>
internal sealed class TenantCreatedDomainEventHandler : IPreCommitDomainEventHandler<TenantCreatedDomainEvent>
{
    private readonly IBus bus;

    public TenantCreatedDomainEventHandler(IBus bus)
    {
        this.bus = bus;
    }

    public async Task HandleAsync(TenantCreatedDomainEvent e, CancellationToken ct = default)
    {
        await bus.PublishAsync(new PayoutOwnerRegisteredEvent(e.TenantId, e.Email), ct);
    }
}
