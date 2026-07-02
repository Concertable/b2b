using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Events;

internal sealed class ConcertCancelledDomainEventHandler : IPreCommitDomainEventHandler<ConcertCancelledDomainEvent>
{
    private readonly IConcertRepository concertRepository;
    private readonly IBus bus;

    public ConcertCancelledDomainEventHandler(IConcertRepository concertRepository, IBus bus)
    {
        this.concertRepository = concertRepository;
        this.bus = bus;
    }

    public async Task HandleAsync(ConcertCancelledDomainEvent e, CancellationToken ct = default)
    {
        var concert = await concertRepository.GetByIdWithBookingAsync(e.ConcertId)
            ?? throw new InvalidOperationException(
                $"Concert {e.ConcertId} not found when publishing ConcertCancelledEvent");

        await bus.PublishAsync(new ConcertCancelledEvent(concert.Booking.ApplicationId, concert.Id), ct);
    }
}
