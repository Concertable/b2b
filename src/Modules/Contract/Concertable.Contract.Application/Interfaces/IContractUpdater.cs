using Concertable.Contract.Contracts;
using Concertable.Contract.Domain.Entities;

namespace Concertable.Contract.Application.Interfaces;

internal interface IContractUpdater
{
    void Apply(ContractEntity existing, IContract source);
}
