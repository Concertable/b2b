using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Workflow;

internal sealed class VersusCalculator : IArtistShareCalculator
{
    public decimal Calculate(IDeal deal, decimal totalRevenue)
    {
        var c = (VersusDeal)deal;
        return c.Guarantee + (totalRevenue * (c.ArtistDoorPercent / 100));
    }
}
