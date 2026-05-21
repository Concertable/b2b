using System.Collections.Frozen;
using Concertable.Contract.Contracts;
using Concertable.Contract.Application.Interfaces;

namespace Concertable.Contract.Infrastructure.Services.Updaters;

internal sealed class ContractUpdater : IContractUpdater
{
    private readonly FrozenDictionary<ContractType, IContractUpdater> updaters;

    public ContractUpdater(
        FlatFeeContractUpdater flatFee,
        DoorSplitContractUpdater doorSplit,
        VersusContractUpdater versus,
        VenueHireContractUpdater venueHire)
    {
        updaters = new Dictionary<ContractType, IContractUpdater>
        {
            [ContractType.FlatFee] = flatFee,
            [ContractType.DoorSplit] = doorSplit,
            [ContractType.Versus] = versus,
            [ContractType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public void Apply(ContractEntity existing, IContract source) =>
        updaters[source.ContractType].Apply(existing, source);
}
