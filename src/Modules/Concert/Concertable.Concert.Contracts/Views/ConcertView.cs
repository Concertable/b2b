using Concertable.Shared;
using NetTopologySuite.Geometries;

namespace Concertable.Concert.Contracts.Views;

public sealed class ConcertView : IIdEntity, IHasName, IHasLocation
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public int VenueId { get; set; }
    public string Name { get; set; } = null!;
    public string About { get; set; } = null!;
    public string? BannerUrl { get; set; }
    public string? Avatar { get; set; }
    public decimal Price { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
    public DateTime? DatePosted { get; set; }
    public Point? Location { get; set; }
    public ICollection<ConcertViewGenre> Genres { get; set; } = [];
}
