using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Booking.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class BookingConfirmedDomainEventHandler
    : IPreCommitDomainEventHandler<BookingConfirmedDomainEvent>
{
    private readonly IBus bus;

    public BookingConfirmedDomainEventHandler(IBus bus)
    {
        this.bus = bus;
    }

    public Task HandleAsync(BookingConfirmedDomainEvent e, CancellationToken ct = default) =>
        bus.PublishAsync(new BookingConfirmedEvent(e.Booking), ct);
}
