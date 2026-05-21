using Concertable.Contract.Contracts;
using Concertable.Contract.Application.Interfaces;

namespace Concertable.Contract.Infrastructure.Services.Updaters;

internal sealed class VersusContractUpdater : IContractUpdater
{
    public void Apply(ContractEntity existing, IContract source)
    {
        var entity = (VersusContractEntity)existing;
        var contract = (VersusContract)source;
        entity.Update(contract.Guarantee, contract.ArtistDoorPercent, contract.PaymentMethod);
    }
}
