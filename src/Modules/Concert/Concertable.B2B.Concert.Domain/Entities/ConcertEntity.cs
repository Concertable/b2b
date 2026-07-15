using System.ComponentModel;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.DataAccess.Application;
using Concertable.Contracts;
using Concertable.Kernel;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// Represents a published concert within the B2B platform.
/// Holds denormalized <see cref="ArtistReadModel"/> and <see cref="VenueReadModel"/> references
/// so the Concert module can satisfy queries in a single DB context without crossing module boundaries.
/// </summary>
[DisplayName("Concert")]
public sealed class ConcertEntity : IIdEntity, IHasName, IHasDateRange, IEventRaiser, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; set; }
    public Guid ArtistTenantId { get; set; }
    public int BookingId { get; private set; }
    public int ArtistId { get; private set; }
    public int VenueId { get; private set; }
    public string Name { get; private set; } = null!;
    public string About { get; private set; } = null!;
    public string? BannerUrl { get; private set; }
    public string? Avatar { get; private set; }
    public decimal Price { get; private set; }
    public int TotalTickets { get; private set; }
    public int TicketsSold { get; private set; }
    public decimal? DoorRevenue { get; private set; }
    public DateRange Period { get; private set; } = null!;
    public DateTime? DatePosted { get; private set; }
    public DealType DealType { get; private set; }
    public BookingEntity Booking { get; set; } = null!;
    public ArtistReadModel Artist { get; set; } = null!;
    public VenueReadModel Venue { get; set; } = null!;
    public List<Genre> Genres { get; private set; } = [];
    public ICollection<ConcertImageEntity> Images { get; private set; } = [];

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    private ConcertEntity() { }

    public static ConcertEntity CreateDraft(
        int bookingId,
        int artistId,
        int venueId,
        DateRange period,
        string name,
        string about,
        DealType dealType,
        IEnumerable<Genre> genres) => new()
        {
            BookingId = bookingId,
            ArtistId = artistId,
            VenueId = venueId,
            Period = period,
            Name = name,
            About = about,
            DealType = dealType,
            Genres = genres.ToList()
        };

    public void IncrementTicketsSold(int quantity) => TicketsSold += quantity;

    /* Venue-declared gross the artist's revenue share settles against (external ticketing + box
       office + cash on the door). A dead night is a real 0m; null means "not yet declared". */
    public void DeclareDoorRevenue(decimal doorRevenue)
    {
        if (doorRevenue < 0)
            throw new DomainException("Door revenue must be zero or greater.");
        DoorRevenue = doorRevenue;
    }

    public void Update(string name, string about, decimal price, int totalTickets)
    {
        Name = name;
        About = about;
        Price = price;
        TotalTickets = totalTickets;
        events.Raise(new ConcertChangedDomainEvent(Id, totalTickets, price, Period, DatePosted));
    }

    public void Post(string name, string about, decimal price, int totalTickets, DateTime now)
    {
        Name = name;
        About = about;
        Price = price;
        TotalTickets = totalTickets;
        DatePosted = now;
        events.Raise(new ConcertChangedDomainEvent(Id, totalTickets, price, Period, now));
        events.Raise(new ConcertPostedDomainEvent(Id));
    }

    public void Cancel() => events.Raise(new ConcertCancelledDomainEvent(Id));
}
