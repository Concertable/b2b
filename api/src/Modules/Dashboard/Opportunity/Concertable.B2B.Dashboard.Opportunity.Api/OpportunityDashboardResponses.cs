using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Dashboard.Opportunity.Api;

internal sealed record OpportunitySummaryResponse(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    DealDto Deal);

internal sealed record OpportunityMetricsResponse(
    OpportunitySummaryResponse Opportunity,
    int ApplicationCount,
    int DaysUntilDeadline);

internal sealed record OpportunityMatchResponse(
    int Id,
    int VenueId,
    string VenueName,
    string County,
    string Town,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    DealDto Deal,
    int FitScore,
    string Href);
