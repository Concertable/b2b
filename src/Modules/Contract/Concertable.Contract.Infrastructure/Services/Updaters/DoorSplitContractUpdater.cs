using Concertable.Contract.Contracts;
using Concertable.Contract.Application.Interfaces;

namespace Concertable.Contract.Infrastructure.Services.Updaters;

internal sealed class DoorSplitContractUpdater : IContractUpdater
{
    public void Apply(ContractEntity existing, IContract source)
    {
        var entity = (DoorSplitContractEntity)existing;
        var contract = (DoorSplitContract)source;
        entity.Update(contract.ArtistDoorPercent, contract.PaymentMethod);
    }
}
