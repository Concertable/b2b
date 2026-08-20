using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class FlatFeeDealMapper : IDealMapper
{
    public DealDto ToDeal(DealEntity entity)
    {
        var e = (FlatFeeDealEntity)entity;
        return new FlatFeeDealDto
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            Fee = e.Fee
        };
    }

    public Result<DealEntity, ValidationErrors> ToEntity(DealDto deal)
    {
        var c = (FlatFeeDealDto)deal;
        return FlatFeeDealEntity.Create(c.Fee, c.PaymentMethod).Map<DealEntity>(entity => entity);
    }
}
