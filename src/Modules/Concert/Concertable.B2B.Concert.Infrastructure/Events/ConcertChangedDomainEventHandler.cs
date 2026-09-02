using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Events;

internal sealed class ConcertChangedDomainEventHandler : IPreCommitDomainEventHandler<ConcertChangedDomainEvent>
{
    private readonly IConcertRepository concertRepository;
    private readonly IBus bus;
    private readonly IDealPayeeResolver dealPayeeResolver;

    public ConcertChangedDomainEventHandler(IConcertRepository concertRepository, IBus bus, IDealPayeeResolver dealPayeeResolver)
    {
        this.concertRepository = concertRepository;
        this.bus = bus;
        this.dealPayeeResolver = dealPayeeResolver;
    }

    public async Task HandleAsync(ConcertChangedDomainEvent e, CancellationToken ct = default)
    {
        var spec = new ConcertSpecification()
            .Include(concert => concert.Artist)
            .Include(concert => concert.Venue)
            .Include(concert => concert.Booking.Application);

        var concert = await concertRepository.GetByIdAsync(e.ConcertId, spec)
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
            dealPayeeResolver.ResolveTicketUserId(concert),
            dealPayeeResolver.ResolveTicketTenantId(concert)), ct);
    }
}
