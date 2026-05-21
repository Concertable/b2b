using Concertable.Contract.Contracts;
using Concertable.Contract.Application.Interfaces;

namespace Concertable.Contract.Infrastructure.Services.Updaters;

internal sealed class VenueHireContractUpdater : IContractUpdater
{
    public void Apply(ContractEntity existing, IContract source)
    {
        var entity = (VenueHireContractEntity)existing;
        var contract = (VenueHireContract)source;
        entity.Update(contract.HireFee, contract.PaymentMethod);
    }
}
