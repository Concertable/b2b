using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record ConcertFinanceResponse(
    int Id,
    int TicketsSold,
    decimal? DoorRevenue,
    bool IsRevenueShare,
    ConcertFinanceActions Actions);
