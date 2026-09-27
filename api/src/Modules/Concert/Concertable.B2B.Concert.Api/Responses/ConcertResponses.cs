using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record PublishedConcertResponse(
    int Id,
    string Name,
    string About,
    DateTime StartsAt,
    DateTime EndsAt,
    string VenueName,
    string ArtistName,
    decimal Price);

internal sealed record SummaryResponse(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string VenueName,
    string ArtistName,
    ConcertState State);

internal sealed record OperationsResponse
{
    public int Id { get; init; }
    public int ApplicationId { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public string? BannerUrl { get; init; }
    public string? Avatar { get; init; }
    public double Rating { get; init; }
    public int TotalTickets { get; init; }
    public int AvailableTickets { get; init; }
    public decimal Price { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? DatePosted { get; init; }
    public ConcertState State { get; init; }
    public required ArtistResponse Artist { get; init; }
    public required VenueResponse Venue { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
    public required ConcertOperationsActions Actions { get; init; }
}

internal sealed record FinanceResponse(
    int Id,
    int TicketsSold,
    decimal? DoorRevenue,
    bool IsRevenueShare,
    ConcertFinanceActions Actions);

internal sealed record ConcertOperationsActions(ActionLink? Cancel);

internal sealed record ConcertFinanceActions(ActionLink? DeclareDoorRevenue, ActionLink? Invoice);

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
