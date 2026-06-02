using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record ConcertDetailsResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public required string BannerUrl { get; init; }
    public required string Avatar { get; init; }
    public double Rating { get; init; }
    public decimal Price { get; init; }
    public int TotalTickets { get; init; }
    public int AvailableTickets { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? DatePosted { get; init; }
    public required ConcertArtistResponse Artist { get; init; }
    public required ConcertVenueResponse Venue { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}

internal sealed record ConcertArtistResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Avatar { get; init; }
    public double Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}

internal sealed record ConcertVenueResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

internal sealed record ConcertSummaryResponse
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
    public required ConcertVenueSummaryResponse Venue { get; init; }
    public required ConcertArtistSummaryResponse Artist { get; init; }
}

internal sealed record ConcertVenueSummaryResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public double Rating { get; init; }
}

internal sealed record ConcertArtistSummaryResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public double Rating { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}
