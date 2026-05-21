using Concertable.Contract.Contracts;
using Concertable.Contract.Application.Interfaces;

namespace Concertable.Contract.Infrastructure.Services.Updaters;

internal sealed class FlatFeeContractUpdater : IContractUpdater
{
    public void Apply(ContractEntity existing, IContract source)
    {
        var entity = (FlatFeeContractEntity)existing;
        var contract = (FlatFeeContract)source;
        entity.Update(contract.Fee, contract.PaymentMethod);
    }
}
