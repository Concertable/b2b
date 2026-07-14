using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record ApplicationResponse(
    int Id,
    ArtistSummary Artist,
    OpportunitySummaryResponse Opportunity,
    ApplicationStatus Status,
    ApplicationActions Actions);

internal sealed record OpportunitySummaryResponse(int Id, DateTime StartDate, DateTime EndDate, IDeal Contract);

internal sealed record ApplicationActions(ActionLink Accept, ActionLink? Checkout, ActionLink? Withdraw, ActionLink? Reject, ActionLink? Cancel, ActionLink? Agreement);
