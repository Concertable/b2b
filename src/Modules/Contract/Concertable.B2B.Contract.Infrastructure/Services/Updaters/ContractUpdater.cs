using System.Collections.Frozen;
using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Infrastructure.Services.Updaters;

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
