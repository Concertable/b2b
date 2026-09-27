using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

internal sealed class TenantDisplayChangedDomainEventHandler
    : IPreCommitDomainEventHandler<TenantDisplayChangedDomainEvent>
{
    private readonly IBus bus;

    public TenantDisplayChangedDomainEventHandler(IBus bus)
    {
        this.bus = bus;
    }

    public Task HandleAsync(TenantDisplayChangedDomainEvent e, CancellationToken ct = default) =>
        this.bus.PublishAsync(new TenantDisplayChanged(
            e.Tenant.Id,
            e.Tenant.DisplayVersion,
            e.Tenant.EffectiveDisplayName), ct);
}
