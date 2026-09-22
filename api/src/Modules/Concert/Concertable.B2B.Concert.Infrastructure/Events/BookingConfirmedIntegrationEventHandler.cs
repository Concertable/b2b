using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.DataAccess.Application;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Events;

internal sealed class BookingConfirmedIntegrationEventHandler : IIntegrationEventHandler<BookingConfirmedEvent>
{
    private readonly IConcertService concertService;
    private readonly ITenantScope tenantScope;

    public BookingConfirmedIntegrationEventHandler(
        IConcertService concertService,
        ITenantScope tenantScope)
    {
        this.concertService = concertService;
        this.tenantScope = tenantScope;
    }

    public Task HandleAsync(
        BookingConfirmedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        using var acting = tenantScope.As(@event.Booking.VenueTenantId);
        return concertService.CreateAsync(@event.Booking, ct);
    }
}
