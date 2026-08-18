using Concertable.B2B.Booking.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Infrastructure.Events;

internal sealed class BookingConfirmedDomainEventHandler : IPreCommitDomainEventHandler<BookingConfirmedDomainEvent>
{
    private readonly IConcertService concerts;

    public BookingConfirmedDomainEventHandler(IConcertService concerts)
    {
        this.concerts = concerts;
    }

    public async Task HandleAsync(BookingConfirmedDomainEvent e, CancellationToken ct = default) =>
        await concerts.CreateAsync(e.Booking, ct);
}
