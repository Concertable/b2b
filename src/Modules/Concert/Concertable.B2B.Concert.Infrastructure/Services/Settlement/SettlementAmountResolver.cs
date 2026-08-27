using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal sealed class SettlementAmountResolver : ISettlementAmountResolver
{
    private readonly IDealTypeStrategyFactory<ISettlementAmountResolver> resolvers;

    public SettlementAmountResolver(IDealTypeStrategyFactory<ISettlementAmountResolver> resolvers)
    {
        this.resolvers = resolvers;
    }

    public Task<Money> ResolveGrossAsync(int concertId, DealDto deal, CancellationToken ct = default) =>
        resolvers.Create(deal.DealType).ResolveGrossAsync(concertId, deal, ct);
}
