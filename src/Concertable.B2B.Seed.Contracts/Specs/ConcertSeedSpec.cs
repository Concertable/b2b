using Concertable.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Seed.Contracts.Specs;

public sealed record ConcertSeedSpec
{
    public required int ConcertId { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public string? Avatar { get; init; }
    public string? BannerUrl { get; init; }
    public required int TotalTickets { get; init; }
    public required int AvailableTickets { get; init; }
    public required decimal Price { get; init; }
    public required DateRange Period { get; init; }
    public DateTime? DatePosted { get; init; }
    public required int ArtistId { get; init; }
    public required string ArtistName { get; init; }
    public required int VenueId { get; init; }
    public required string VenueName { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required IReadOnlyCollection<Genre> Genres { get; init; }
    public required Guid PayeeUserId { get; init; }

    public static ConcertSeedSpec Create(
        int id,
        string name,
        decimal price,
        int total,
        ArtistSeedSpec artist,
        VenueSeedSpec venue,
        int daysOffset,
        int datePostedDaysOffset,
        DateTime now,
        IReadOnlyCollection<Genre>? genres = null) => new()
    {
        ConcertId = id,
        Name = name,
        About = $"{name} is a concert at {venue.Name}.",
        Avatar = null,
        BannerUrl = null,
        TotalTickets = total,
        AvailableTickets = total,
        Price = price,
        Period = new DateRange(now.AddDays(daysOffset), now.AddDays(daysOffset).AddHours(3)),
        DatePosted = now.AddDays(datePostedDaysOffset),
        ArtistId = artist.ArtistId,
        ArtistName = artist.Name,
        VenueId = venue.VenueId,
        VenueName = venue.Name,
        Latitude = venue.Latitude,
        Longitude = venue.Longitude,
        Genres = genres ?? [],
        PayeeUserId = venue.UserId,
    };

    public static ConcertSeedSpec CreateHire(
        int id,
        string name,
        decimal price,
        int total,
        ArtistSeedSpec artist,
        VenueSeedSpec venue,
        int daysOffset,
        int datePostedDaysOffset,
        DateTime now,
        IReadOnlyCollection<Genre>? genres = null) =>
        Create(id, name, price, total, artist, venue, daysOffset, datePostedDaysOffset, now, genres)
            with { PayeeUserId = artist.UserId };
}
