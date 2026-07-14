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
