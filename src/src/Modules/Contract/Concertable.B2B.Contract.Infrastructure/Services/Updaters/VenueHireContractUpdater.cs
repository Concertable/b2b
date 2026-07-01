using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Infrastructure.Services.Updaters;

internal sealed class VenueHireContractUpdater : IContractUpdater
{
    public void Apply(ContractEntity existing, IContract source)
    {
        var entity = (VenueHireContractEntity)existing;
        var contract = (VenueHireContract)source;
        entity.Update(contract.HireFee, contract.PaymentMethod);
    }
}
