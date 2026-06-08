using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Workflow;

internal sealed class DoorSplitCalculator : IArtistShareCalculator
{
    public decimal Calculate(IContract contract, decimal totalRevenue)
    {
        var c = (DoorSplitContract)contract;
        return totalRevenue * (c.ArtistDoorPercent / 100);
    }
}
