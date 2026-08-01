using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class VersusDealMapper : IDealMapper
{
    public IDeal ToDeal(DealEntity entity)
    {
        var e = (VersusDealEntity)entity;
        return new VersusDeal
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            Guarantee = e.Guarantee,
            ArtistDoorPercent = e.ArtistDoorPercent
        };
    }

    public Result<DealEntity, ValidationErrors> ToEntity(IDeal deal)
    {
        var c = (VersusDeal)deal;
        return VersusDealEntity.Create(c.Guarantee, c.ArtistDoorPercent, c.PaymentMethod).Map<DealEntity>(entity => entity);
    }
}
