using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal sealed class SettlementAmountResolver : ISettlementAmountResolver
{
    private readonly IDealStrategyFactory<ISettlementAmountResolver> amountResolverFactory;

    public SettlementAmountResolver(IDealStrategyFactory<ISettlementAmountResolver> amountResolverFactory)
    {
        this.amountResolverFactory = amountResolverFactory;
    }

    public Task<Money> ResolveGrossAsync(int concertId, DealDto deal, CancellationToken ct = default) =>
        amountResolverFactory.Create(deal.DealType).ResolveGrossAsync(concertId, deal, ct);
}
