using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Application.Interfaces;

internal interface IContractUpdater
{
    void Apply(ContractEntity existing, IContract source);
}
