using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record ConcertOperationsResponse
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
