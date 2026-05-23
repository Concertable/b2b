using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Application.Interfaces;

internal interface IContractMapper
{
    IContract ToContract(ContractEntity entity);
    ContractEntity ToEntity(IContract contract);
}
