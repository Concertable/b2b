using Concertable.Contracts;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record ConcertDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string About { get; set; }
    public string? BannerUrl { get; set; }
    public string? Avatar { get; set; }
    public double Rating { get; set; }
    public decimal Price { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? DatePosted { get; set; }
    public required ConcertVenueDto Venue { get; set; }
    public required ConcertArtistDto Artist { get; set; }
    public IEnumerable<Genre> Genres { get; set; } = [];
}

internal sealed record ConcertVenueDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public double Rating { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

internal sealed record ConcertArtistDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Avatar { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public double Rating { get; set; }
    public IEnumerable<Genre> Genres { get; set; } = [];
}

internal sealed record ConcertSummaryDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? DatePosted { get; set; }
    public required ConcertVenueSummaryDto Venue { get; set; }
    public required ConcertArtistSummaryDto Artist { get; set; }
}

internal sealed record ConcertVenueSummaryDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public double Rating { get; set; }
}

internal sealed record ConcertArtistSummaryDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public double Rating { get; set; }
    public IEnumerable<Genre> Genres { get; set; } = [];
}

internal sealed record ConcertSnapshot
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ImageUrl { get; set; }
    public double? Rating { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? DatePosted { get; set; }
    public IReadOnlyList<Genre> Genres { get; set; } = [];
}

