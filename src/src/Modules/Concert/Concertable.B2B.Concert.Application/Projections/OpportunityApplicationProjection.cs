using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.Application.Projections;

internal sealed record OpportunityApplicationProjection
{
    public int Id { get; init; }
    public int VenueId { get; init; }
    public required string VenueName { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public List<Genre> Genres { get; init; } = [];
    public int DealId { get; init; }
    public int ApplicationCount { get; init; }
}
