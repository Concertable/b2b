using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal sealed class VenueHireSettlementAmount : ISettlementAmountResolver
{
    public Task<Money> ResolveGrossAsync(int concertId, DealDto deal, CancellationToken ct = default) =>
        Task.FromResult(Money.Gbp(((VenueHireDealDto)deal).HireFee));
}
