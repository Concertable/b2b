using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record ApplicationResponse(
    int Id,
    ArtistSummary Artist,
    OpportunitySummaryResponse Opportunity,
    ApplicationStatus Status,
    ApplicationActions Actions);

internal sealed record OpportunitySummaryResponse(int Id, DateTime StartDate, DateTime EndDate, IContract Contract);

internal sealed record ApplicationActions(ActionLink Accept, ActionLink? Checkout);
