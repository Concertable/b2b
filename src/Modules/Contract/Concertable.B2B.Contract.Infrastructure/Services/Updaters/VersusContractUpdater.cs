using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Infrastructure.Services.Updaters;

internal sealed class VersusContractUpdater : IContractUpdater
{
    public void Apply(ContractEntity existing, IContract source)
    {
        var entity = (VersusContractEntity)existing;
        var contract = (VersusContract)source;
        entity.Update(contract.Guarantee, contract.ArtistDoorPercent, contract.PaymentMethod);
    }
}
