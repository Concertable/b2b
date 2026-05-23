using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Infrastructure.Services.Updaters;

internal sealed class FlatFeeContractUpdater : IContractUpdater
{
    public void Apply(ContractEntity existing, IContract source)
    {
        var entity = (FlatFeeContractEntity)existing;
        var contract = (FlatFeeContract)source;
        entity.Update(contract.Fee, contract.PaymentMethod);
    }
}
