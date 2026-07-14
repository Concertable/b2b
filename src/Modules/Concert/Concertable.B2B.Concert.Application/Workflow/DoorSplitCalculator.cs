using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Workflow;

internal sealed class DoorSplitCalculator : IArtistShareCalculator
{
    public decimal Calculate(IDeal deal, decimal totalRevenue)
    {
        var c = (DoorSplitDeal)deal;
        return totalRevenue * (c.ArtistDoorPercent / 100);
    }
}
