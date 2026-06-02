using Concertable.Contracts;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record ConcertDetails
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public string? BannerUrl { get; init; }
    public string? Avatar { get; init; }
    public double Rating { get; init; }
    public decimal Price { get; init; }
    public int TotalTickets { get; init; }
    public int AvailableTickets { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? DatePosted { get; init; }
    public required ConcertVenue Venue { get; init; }
    public required ConcertArtist Artist { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
}

internal sealed record ConcertVenue
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public double Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

internal sealed record ConcertArtist
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Avatar { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public double Rating { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
}

internal sealed record ConcertSummary
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
    public int TotalTickets { get; init; }
    public int AvailableTickets { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? DatePosted { get; init; }
    public required ConcertVenueSummary Venue { get; init; }
    public required ConcertArtistSummary Artist { get; init; }
}

internal sealed record ConcertVenueSummary(int Id, string Name, double Rating);

internal sealed record ConcertArtistSummary
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public double Rating { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
}

internal sealed record ConcertSnapshot
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string ImageUrl { get; init; }
    public double? Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? DatePosted { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}
