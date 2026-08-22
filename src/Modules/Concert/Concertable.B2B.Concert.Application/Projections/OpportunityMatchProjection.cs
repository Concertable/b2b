using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.Application.Projections;

internal sealed record OpportunityMatchProjection
{
    public int Id { get; init; }
    public int VenueId { get; init; }
    public required string VenueName { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public List<Genre> Genres { get; init; } = [];
    public int DealId { get; init; }
}
