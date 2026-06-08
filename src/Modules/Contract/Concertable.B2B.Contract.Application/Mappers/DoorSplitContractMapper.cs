using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Application.Mappers;

internal sealed class DoorSplitContractMapper : IContractMapper
{
    public IContract ToContract(ContractEntity entity)
    {
        var e = (DoorSplitContractEntity)entity;
        return new DoorSplitContract
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            ArtistDoorPercent = e.ArtistDoorPercent
        };
    }

    public ContractEntity ToEntity(IContract contract)
    {
        var c = (DoorSplitContract)contract;
        return DoorSplitContractEntity.Create(c.ArtistDoorPercent, c.PaymentMethod);
    }
}
