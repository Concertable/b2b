using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Workflow;

internal sealed class VersusCalculator : IArtistShareCalculator
{
    public decimal Calculate(IContract contract, decimal totalRevenue)
    {
        var c = (VersusContract)contract;
        return c.Guarantee + (totalRevenue * (c.ArtistDoorPercent / 100));
    }
}
