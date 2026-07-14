using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class FlatFeeDealMapper : IDealMapper
{
    public IDeal ToDeal(DealEntity entity)
    {
        var e = (FlatFeeDealEntity)entity;
        return new FlatFeeDeal
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            Fee = e.Fee
        };
    }

    public DealEntity ToEntity(IDeal contract)
    {
        var c = (FlatFeeDeal)contract;
        return FlatFeeDealEntity.Create(c.Fee, c.PaymentMethod);
    }
}
