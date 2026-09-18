using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Events;

internal sealed class ConcertChangedDomainEventHandler : IPreCommitDomainEventHandler<ConcertChangedDomainEvent>
{
    private readonly IConcertPrivilegedRepository concertRepository;
    private readonly IBus bus;

    public ConcertChangedDomainEventHandler(IConcertPrivilegedRepository concertRepository, IBus bus)
    {
        this.concertRepository = concertRepository;
        this.bus = bus;
    }

    public async Task HandleAsync(ConcertChangedDomainEvent e, CancellationToken ct = default)
    {
        var spec = new ConcertSpecification()
            .Include(concert => concert.Artist)
            .Include(concert => concert.Venue);

        var concert = await concertRepository.GetByIdAsync(e.ConcertId, spec, ct)
            ?? throw new InvalidOperationException(
                $"Concert {e.ConcertId} not found when publishing ConcertChangedEvent");

        var artist = concert.Artist;
        var venue = concert.Venue;

        await bus.PublishAsync(new ConcertChangedEvent(
            concert.Id,
            concert.Name,
            concert.About,
            concert.Avatar,
            concert.BannerUrl,
            concert.TotalTickets,
            concert.TotalTickets - concert.TicketsSold,
            e.Price,
            e.Period,
            e.DatePosted,
            artist.Id,
            artist.Name,
            venue.Id,
            venue.Name,
            venue.Location.Y,
            venue.Location.X,
            concert.Genres.ToArray(),
            /* Whoever pays the performer is whoever sold the tickets, and the concert type already says
               which side that is — VenueHire reverses it. P2 replaces both with the accepted settlement
               binding, which is where a direction that is agreed rather than derived belongs. */
            concert.SettlementPayerTenantId == concert.VenueTenantId ? venue.UserId : artist.UserId,
            concert.SettlementPayerTenantId), ct);
    }
}
