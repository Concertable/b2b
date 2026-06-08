using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Application.Interfaces;

internal interface IContractMapper
{
    IContract ToContract(ContractEntity entity);
    ContractEntity ToEntity(IContract contract);

    IEnumerable<IContract> ToContracts(IEnumerable<ContractEntity> entities) => entities.Select(ToContract);
    IEnumerable<ContractEntity> ToEntities(IEnumerable<IContract> contracts) => contracts.Select(ToEntity);
}
