using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Application.Mappers;

internal sealed class VenueHireContractMapper : IContractMapper
{
    public IContract ToContract(ContractEntity entity)
    {
        var e = (VenueHireContractEntity)entity;
        return new VenueHireContract
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            HireFee = e.HireFee
        };
    }

    public ContractEntity ToEntity(IContract contract)
    {
        var c = (VenueHireContract)contract;
        return VenueHireContractEntity.Create(c.HireFee, c.PaymentMethod);
    }
}
