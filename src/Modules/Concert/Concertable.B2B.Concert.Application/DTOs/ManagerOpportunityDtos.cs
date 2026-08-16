using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record OpportunityListRow
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
    public int ApplicationCount { get; init; }
}

internal sealed record ManagerOpportunitySummary(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    IDeal Deal);

internal sealed record VenueOpenOpportunity(
    ManagerOpportunitySummary Opportunity,
    int ApplicationCount,
    int DaysUntilDeadline);

internal sealed record RecommendedOpportunity(
    int Id,
    int VenueId,
    string VenueName,
    string County,
    string Town,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    IDeal Deal,
    int FitScore,
    string Href);
