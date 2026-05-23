using Concertable.Contract.Domain.Entities;

namespace Concertable.Contract.Application.Interfaces;

internal interface IContractMapper
{
    IContract ToContract(ContractEntity entity);
    ContractEntity ToEntity(IContract contract);
}
