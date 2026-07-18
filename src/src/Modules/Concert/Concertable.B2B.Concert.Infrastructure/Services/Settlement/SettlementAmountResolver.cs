using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal sealed class SettlementAmountResolver : ISettlementAmountResolver
{
    private readonly FrozenDictionary<DealType, ISettlementAmountResolver> resolvers;

    public SettlementAmountResolver(
        FlatFeeSettlementAmount flatFee,
        VenueHireSettlementAmount venueHire,
        RevenueShareSettlementAmount revenueShare)
    {
        resolvers = new Dictionary<DealType, ISettlementAmountResolver>
        {
            [DealType.FlatFee] = flatFee,
            [DealType.VenueHire] = venueHire,
            [DealType.DoorSplit] = revenueShare,
            [DealType.Versus] = revenueShare,
        }.ToFrozenDictionary();
    }

    public Task<decimal> ResolveGrossAsync(int concertId, IDeal deal, CancellationToken ct = default) =>
        resolvers[deal.DealType].ResolveGrossAsync(concertId, deal, ct);
}
