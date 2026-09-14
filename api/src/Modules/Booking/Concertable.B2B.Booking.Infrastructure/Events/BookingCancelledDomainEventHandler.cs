using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Booking.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class BookingCancelledDomainEventHandler
    : IPreCommitDomainEventHandler<BookingCancelledDomainEvent>
{
    private readonly IBus bus;

    public BookingCancelledDomainEventHandler(IBus bus)
    {
        this.bus = bus;
    }

    public Task HandleAsync(BookingCancelledDomainEvent e, CancellationToken ct = default) =>
        bus.PublishAsync(new BookingCancelledEvent(e.BookingId, e.ApplicationId, e.OpportunityId), ct);
}
