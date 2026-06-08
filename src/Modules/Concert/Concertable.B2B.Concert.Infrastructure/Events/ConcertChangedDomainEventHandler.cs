using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Events;

internal sealed class ConcertChangedDomainEventHandler : IPreCommitDomainEventHandler<ConcertChangedDomainEvent>
{
    private readonly IConcertRepository concertRepository;
    private readonly IBus bus;

    public ConcertChangedDomainEventHandler(IConcertRepository concertRepository, IBus bus)
    {
        this.concertRepository = concertRepository;
        this.bus = bus;
    }

    public async Task HandleAsync(ConcertChangedDomainEvent e, CancellationToken ct = default)
    {
        var concert = await concertRepository.GetByIdWithArtistAndVenueAsync(e.ConcertId)
            ?? throw new InvalidOperationException(
                $"Concert {e.ConcertId} not found when publishing ConcertChangedEvent");

        var artist = concert.Artist;
        var venue = concert.Venue;
        var payeeUserId = concert.ContractType == ContractType.VenueHire
            ? artist.UserId
            : venue.UserId;

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
            payeeUserId), ct);
    }
}
