using Concertable.B2B.Contract.Contracts;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal record OpportunityResponse(
    int Id,
    int VenueId,
    IContract Contract,
    DateTime StartDate,
    DateTime EndDate,
    IEnumerable<Genre> Genres,
    OpportunityActions Actions);

internal record OpportunityActions(ActionLink? Checkout);
