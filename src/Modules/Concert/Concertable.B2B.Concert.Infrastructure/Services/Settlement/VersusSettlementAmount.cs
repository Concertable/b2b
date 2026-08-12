using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal sealed class VersusSettlementAmount : RevenueShareSettlementAmount
{
    public VersusSettlementAmount(IConcertRepository concertRepository) : base(concertRepository) { }

    protected override decimal CalculateGross(IDeal deal, decimal totalRevenue)
    {
        var versus = (VersusDeal)deal;
        return versus.Guarantee + (totalRevenue * (versus.ArtistDoorPercent / 100));
    }
}
