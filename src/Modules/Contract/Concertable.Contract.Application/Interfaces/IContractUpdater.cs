using Concertable.Contract.Contracts;

namespace Concertable.Contract.Application.Interfaces;

internal interface IContractUpdater
{
    void Apply(ContractEntity existing, IContract source);
}
