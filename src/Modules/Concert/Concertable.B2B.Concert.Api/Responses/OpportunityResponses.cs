using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record OpportunityResponse(
    int Id,
    int VenueId,
    IDeal Deal,
    DateTime StartDate,
    DateTime EndDate,
    IEnumerable<Genre> Genres,
    OpportunityActions Actions);

internal sealed record OpportunityActions(ActionLink? Checkout);

internal sealed record OpportunityApplicationMetricsResponse(
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
    IDeal Deal,
    int FitScore,
    string Href);
