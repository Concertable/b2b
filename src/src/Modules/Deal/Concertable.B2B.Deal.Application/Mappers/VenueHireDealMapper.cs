using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

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

    public Result<DealEntity, ValidationErrors> ToEntity(IDeal deal)
    {
        var c = (VenueHireDeal)deal;
        return VenueHireDealEntity.Create(c.HireFee, c.PaymentMethod).Map<DealEntity>(entity => entity);
    }
}
