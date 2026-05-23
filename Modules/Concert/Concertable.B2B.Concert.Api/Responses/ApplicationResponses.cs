using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Domain.Enums;
using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal record ApplicationResponse(
    int Id,
    ArtistSummaryDto Artist,
    OpportunitySummaryResponse Opportunity,
    ApplicationStatus Status,
    ApplicationActions Actions);

internal record OpportunitySummaryResponse(int Id, DateTime StartDate, DateTime EndDate, IContract Contract);

internal record ApplicationActions(ActionLink Accept, ActionLink? Checkout);
