using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Application.Mappers;

internal sealed class VersusContractMapper : IContractMapper
{
    public IContract ToContract(ContractEntity entity)
    {
        var e = (VersusContractEntity)entity;
        return new VersusContract
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            Guarantee = e.Guarantee,
            ArtistDoorPercent = e.ArtistDoorPercent
        };
    }

    public ContractEntity ToEntity(IContract contract)
    {
        var c = (VersusContract)contract;
        return VersusContractEntity.Create(c.Guarantee, c.ArtistDoorPercent, c.PaymentMethod);
    }
}
