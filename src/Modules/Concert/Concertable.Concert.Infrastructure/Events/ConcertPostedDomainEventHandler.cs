using Concertable.Concert.Contracts.Events;
using Concertable.Concert.Domain.Events;

namespace Concertable.Concert.Infrastructure.Events;

internal class ConcertPostedDomainEventHandler : IPreCommitDomainEventHandler<ConcertPostedDomainEvent>
{
    private readonly IConcertRepository concertRepository;
    private readonly IBus bus;

    public ConcertPostedDomainEventHandler(IConcertRepository concertRepository, IBus bus)
    {
        this.concertRepository = concertRepository;
        this.bus = bus;
    }

    public async Task HandleAsync(ConcertPostedDomainEvent e, CancellationToken ct = default)
    {
        var concert = await concertRepository.GetFullByIdAsync(e.ConcertId)
            ?? throw new InvalidOperationException(
                $"Concert {e.ConcertId} not found when publishing ConcertPostedEvent");

        var venue = concert.Booking.Application.Opportunity.Venue;

        await bus.PublishAsync(new ConcertPostedEvent(
            concert.Id,
            concert.Name,
            concert.Avatar,
            concert.Price,
            concert.Period,
            concert.DatePosted!.Value,
            venue.Location?.Y,
            venue.Location?.X,
            concert.Genres.ToArray()), ct);
    }
}
