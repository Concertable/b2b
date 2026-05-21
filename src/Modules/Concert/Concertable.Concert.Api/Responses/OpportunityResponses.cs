using Concertable.Contract.Contracts;

namespace Concertable.Concert.Api.Responses;

internal record OpportunityResponse(
    int Id,
    int VenueId,
    IContract Contract,
    DateTime StartDate,
    DateTime EndDate,
    IEnumerable<Genre> Genres,
    OpportunityActions Actions);

internal record OpportunityActions(ActionLink? Checkout);
