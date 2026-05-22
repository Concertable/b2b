using Concertable.Concert.Domain.Events;
using Concertable.Shared;
using NetTopologySuite.Geometries;

namespace Concertable.Concert.Domain;

public class ConcertEntity : IIdEntity, IHasName, IHasLocation, IHasDateRange, ILifecycleEntity, IEventRaiser
{
    public int Id { get; private set; }
    public int BookingId { get; private set; }
    public int ArtistId { get; private set; }
    public int VenueId { get; private set; }
    public string Name { get; private set; } = null!;
    public string About { get; private set; } = null!;
    public string? BannerUrl { get; private set; }
    public string? Avatar { get; private set; }
    public decimal Price { get; private set; }
    public int TotalTickets { get; private set; }
    public DateRange Period { get; private set; } = null!;
    public DateTime? DatePosted { get; private set; }
    public Point? Location { get; set; }
    public ContractType ContractType { get; private set; }
    public ConcertStage CurrentStage { get; private set; } = ConcertStage.Settled;
    public BookingEntity Booking { get; set; } = null!;
    public ArtistReadModel Artist { get; set; } = null!;
    public VenueReadModel Venue { get; set; } = null!;
    public List<Genre> Genres { get; private set; } = [];
    public ICollection<ConcertImageEntity> Images { get; private set; } = [];

    private readonly EventRaiser _events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _events.DomainEvents;
    public void ClearDomainEvents() => _events.Clear();

    private ConcertEntity() { }

    public static ConcertEntity CreateDraft(
        int bookingId,
        int artistId,
        int venueId,
        DateRange period,
        string name,
        string about,
        IEnumerable<Genre> genres) => new()
    {
        BookingId = bookingId,
        ArtistId = artistId,
        VenueId = venueId,
        Period = period,
        Name = name,
        About = about,
        Genres = genres.ToList()
    };

    public static ConcertEntity CreateDraft(
        int bookingId,
        int artistId,
        int venueId,
        DateRange period,
        string name,
        string about,
        ContractType contractType,
        IEnumerable<Genre> genres) => new()
    {
        BookingId = bookingId,
        ArtistId = artistId,
        VenueId = venueId,
        Period = period,
        Name = name,
        About = about,
        ContractType = contractType,
        Genres = genres.ToList()
    };

    public void AdvanceStage(ConcertStage next)
    {
        if (next is not (ConcertStage.Settled or ConcertStage.Finished))
            throw new DomainException($"ConcertEntity cannot advance to {next}.");
        CurrentStage = next;
    }

    public void Update(string name, string about, decimal price, int totalTickets)
    {
        Name = name;
        About = about;
        Price = price;
        TotalTickets = totalTickets;
        _events.Raise(new ConcertChangedDomainEvent(Id, totalTickets, price, Period, DatePosted));
    }

    public void Post(string name, string about, decimal price, int totalTickets, DateTime now)
    {
        Name = name;
        About = about;
        Price = price;
        TotalTickets = totalTickets;
        DatePosted = now;
        _events.Raise(new ConcertChangedDomainEvent(Id, totalTickets, price, Period, now));
        _events.Raise(new ConcertPostedDomainEvent(Id));
    }
}
