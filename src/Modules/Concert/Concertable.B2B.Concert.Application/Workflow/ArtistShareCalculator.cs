using System.Collections.Frozen;
using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Workflow;

internal sealed class ArtistShareCalculator : IArtistShareCalculator
{
    private readonly FrozenDictionary<ContractType, IArtistShareCalculator> calculators;

    public ArtistShareCalculator(DoorSplitCalculator doorSplit, VersusCalculator versus)
    {
        calculators = new Dictionary<ContractType, IArtistShareCalculator>
        {
            [ContractType.DoorSplit] = doorSplit,
            [ContractType.Versus] = versus,
        }.ToFrozenDictionary();
    }

    public decimal Calculate(IContract contract, decimal totalRevenue) =>
        calculators[contract.ContractType].Calculate(contract, totalRevenue);
}
