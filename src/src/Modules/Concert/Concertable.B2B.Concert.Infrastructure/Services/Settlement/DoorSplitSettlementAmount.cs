using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal sealed class DoorSplitSettlementAmount : RevenueShareSettlementAmount
{
    public DoorSplitSettlementAmount(IConcertRepository concertRepository) : base(concertRepository) { }

    protected override decimal CalculateGross(IDeal deal, decimal totalRevenue)
    {
        var doorSplit = (DoorSplitDeal)deal;
        return totalRevenue * (doorSplit.ArtistDoorPercent / 100);
    }
}
