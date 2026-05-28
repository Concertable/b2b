using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Events;

internal class ConcertChangedDomainEventHandler(
    IConcertRepository concertRepository,
    IBus bus)
    : IPreCommitDomainEventHandler<ConcertChangedDomainEvent>
{
    public async Task HandleAsync(ConcertChangedDomainEvent e, CancellationToken ct = default)
    {
        var concert = await concertRepository.GetFullByIdAsync(e.ConcertId)
            ?? throw new InvalidOperationException(
                $"Concert {e.ConcertId} not found when publishing ConcertChangedEvent");

        var artist = concert.Booking.Application.Artist;
        var venue = concert.Booking.Application.Opportunity.Venue;
        var payeeUserId = concert.ContractType == ContractType.VenueHire
            ? artist.UserId
            : venue.UserId;

        await bus.PublishAsync(new ConcertChangedEvent(
            concert.Id,
            concert.Name,
            concert.About,
            concert.Avatar,
            concert.BannerUrl,
            e.TotalTickets,
            e.TotalTickets,
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
