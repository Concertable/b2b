using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record DetailsResponse
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
    public required ArtistResponse Artist { get; init; }
    public required VenueResponse Venue { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}

internal sealed record MyDetailsResponse
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
    public required ArtistResponse Artist { get; init; }
    public required VenueResponse Venue { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
    public int TicketsSold { get; init; }
    public decimal? DoorRevenue { get; init; }
    public required ConcertActions Actions { get; init; }
}

internal sealed record ConcertActions(ActionLink? Cancel, ActionLink? Contract, ActionLink? DeclareDoorRevenue, ActionLink? Invoice);

internal sealed record ArtistResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Avatar { get; init; }
    public double Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}

internal sealed record VenueResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

internal sealed record SummaryResponse
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
    public required VenueSummaryResponse Venue { get; init; }
    public required ArtistSummaryResponse Artist { get; init; }
}

internal sealed record VenueSummaryResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public double Rating { get; init; }
}

internal sealed record ArtistSummaryResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public double Rating { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}
