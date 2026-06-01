using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record ConcertDetailsResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string About { get; set; }
    public required string BannerUrl { get; set; }
    public required string Avatar { get; set; }
    public double Rating { get; set; }
    public decimal Price { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? DatePosted { get; set; }
    public required ConcertArtistResponse Artist { get; set; }
    public required ConcertVenueResponse Venue { get; set; }
    public IReadOnlyList<Genre> Genres { get; set; } = [];
}

internal sealed record ConcertArtistResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Avatar { get; set; }
    public double Rating { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public IReadOnlyList<Genre> Genres { get; set; } = [];
}

internal sealed record ConcertVenueResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

internal sealed record ConcertSummaryResponse
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
    public required ConcertVenueSummaryResponse Venue { get; set; }
    public required ConcertArtistSummaryResponse Artist { get; set; }
}

internal sealed record ConcertVenueSummaryResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public double Rating { get; set; }
}

internal sealed record ConcertArtistSummaryResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public double Rating { get; set; }
    public IReadOnlyList<Genre> Genres { get; set; } = [];
}
