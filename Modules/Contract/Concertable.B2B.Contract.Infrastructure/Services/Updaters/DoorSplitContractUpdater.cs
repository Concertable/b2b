using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Infrastructure.Services.Updaters;

internal sealed class DoorSplitContractUpdater : IContractUpdater
{
    public void Apply(ContractEntity existing, IContract source)
    {
        var entity = (DoorSplitContractEntity)existing;
        var contract = (DoorSplitContract)source;
        entity.Update(contract.ArtistDoorPercent, contract.PaymentMethod);
    }
}
