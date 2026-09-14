using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal abstract class RevenueShareSettlementAmount : ISettlementAmountResolver
{
    private readonly IConcertRepository concertRepository;

    protected RevenueShareSettlementAmount(IConcertRepository concertRepository)
    {
        this.concertRepository = concertRepository;
    }

    public async Task<Money> ResolveGrossAsync(int concertId, DealDto deal, CancellationToken ct = default)
    {
        var totalRevenue = await concertRepository.GetTotalRevenueByConcertIdAsync(concertId)
            ?? throw new InvalidOperationException(
                $"Concert {concertId} reached settlement with no declared door revenue — the completion gate should make this unreachable.");
        return Money.Gbp(CalculateGross(deal, totalRevenue));
    }

    protected abstract decimal CalculateGross(DealDto deal, decimal totalRevenue);
}
