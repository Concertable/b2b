using System.Collections.Frozen;
using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Application.Mappers;

internal sealed class ContractMapper : IContractMapper
{
    private readonly FrozenDictionary<ContractType, IContractMapper> mappers;

    public ContractMapper(
        FlatFeeContractMapper flatFee,
        DoorSplitContractMapper doorSplit,
        VersusContractMapper versus,
        VenueHireContractMapper venueHire)
    {
        mappers = new Dictionary<ContractType, IContractMapper>
        {
            [ContractType.FlatFee] = flatFee,
            [ContractType.DoorSplit] = doorSplit,
            [ContractType.Versus] = versus,
            [ContractType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public IContract ToContract(ContractEntity entity) =>
        mappers[entity.ContractType].ToContract(entity);

    public ContractEntity ToEntity(IContract contract) =>
        mappers[contract.ContractType].ToEntity(contract);
}
