using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class VenueHireDealMapper : IDealMapper
{
    public IDeal ToDeal(DealEntity entity)
    {
        var e = (VenueHireDealEntity)entity;
        return new VenueHireDeal
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            HireFee = e.HireFee
        };
    }

    public DealEntity ToEntity(IDeal contract)
    {
        var c = (VenueHireDeal)contract;
        return VenueHireDealEntity.Create(c.HireFee, c.PaymentMethod);
    }
}
