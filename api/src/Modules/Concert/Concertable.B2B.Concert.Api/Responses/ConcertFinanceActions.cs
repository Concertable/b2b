using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record ConcertFinanceActions(ActionLink? DeclareDoorRevenue, ActionLink? Invoice);
