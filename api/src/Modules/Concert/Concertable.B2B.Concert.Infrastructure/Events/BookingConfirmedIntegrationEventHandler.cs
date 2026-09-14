using Concertable.B2B.Booking.Contracts.Events;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Events;

internal sealed class BookingConfirmedIntegrationEventHandler : IIntegrationEventHandler<BookingConfirmedEvent>
{
    private readonly IConcertService concertService;

    public BookingConfirmedIntegrationEventHandler(IConcertService concertService)
    {
        this.concertService = concertService;
    }

    public Task HandleAsync(
        BookingConfirmedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        concertService.CreateAsync(@event.Booking, ct);
}
