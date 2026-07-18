using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal sealed class RevenueShareSettlementAmount : ISettlementAmountResolver
{
    private readonly IConcertRepository concertRepository;
    private readonly IArtistShareCalculator artistShareCalculator;

    public RevenueShareSettlementAmount(
        IConcertRepository concertRepository,
        IArtistShareCalculator artistShareCalculator)
    {
        this.concertRepository = concertRepository;
        this.artistShareCalculator = artistShareCalculator;
    }

    public async Task<decimal> ResolveGrossAsync(int concertId, IDeal deal, CancellationToken ct = default)
    {
        var totalRevenue = await concertRepository.GetTotalRevenueByConcertIdAsync(concertId)
            ?? throw new InvalidOperationException(
                $"Concert {concertId} reached settlement with no declared door revenue — the completion gate should make this unreachable.");
        return artistShareCalculator.Calculate(deal, totalRevenue);
    }
}
