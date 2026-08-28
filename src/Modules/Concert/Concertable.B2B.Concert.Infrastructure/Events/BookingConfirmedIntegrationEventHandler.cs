using Concertable.B2B.Booking.Contracts.Events;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Events;

internal sealed class BookingConfirmedIntegrationEventHandler : IIntegrationEventHandler<BookingConfirmedEvent>
{
    private readonly IConcertService concerts;

    public BookingConfirmedIntegrationEventHandler(IConcertService concerts)
    {
        this.concerts = concerts;
    }

    public Task HandleAsync(
        BookingConfirmedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        concerts.CreateAsync(@event.Booking, ct);
}
