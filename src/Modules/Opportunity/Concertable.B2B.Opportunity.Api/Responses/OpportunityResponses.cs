using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts;

namespace Concertable.B2B.Opportunity.Api.Responses;

internal sealed record OpportunityResponse(
    int Id,
    int VenueId,
    DealDto Deal,
    DateTime StartDate,
    DateTime EndDate,
    IEnumerable<Genre> Genres,
    OpportunityActions Actions);

internal sealed record OpportunityActions(ActionLink? Checkout);

internal sealed record OpportunitySummaryResponse(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    DealDto Deal);
