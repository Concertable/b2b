using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal sealed class VenueHireSettlementAmount : ISettlementAmountResolver
{
    public Task<decimal> ResolveGrossAsync(int concertId, IDeal deal, CancellationToken ct = default) =>
        Task.FromResult(((VenueHireDeal)deal).HireFee);
}
