using System.Collections.Frozen;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Workflow;

internal sealed class ArtistShareCalculator : IArtistShareCalculator
{
    private readonly FrozenDictionary<DealType, IArtistShareCalculator> calculators;

    public ArtistShareCalculator(DoorSplitCalculator doorSplit, VersusCalculator versus)
    {
        calculators = new Dictionary<DealType, IArtistShareCalculator>
        {
            [DealType.DoorSplit] = doorSplit,
            [DealType.Versus] = versus,
        }.ToFrozenDictionary();
    }

    public decimal Calculate(IDeal deal, decimal totalRevenue) =>
        calculators[deal.DealType].Calculate(deal, totalRevenue);
}
