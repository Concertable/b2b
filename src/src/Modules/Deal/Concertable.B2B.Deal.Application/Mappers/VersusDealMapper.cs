using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class VersusDealMapper : IDealMapper
{
    public DealDto ToDeal(DealEntity entity)
    {
        var e = (VersusDealEntity)entity;
        return new VersusDealDto
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            Guarantee = e.Guarantee,
            ArtistDoorPercent = e.ArtistDoorPercent
        };
    }

    public Result<DealEntity, ValidationErrors> ToEntity(DealDto deal)
    {
        var c = (VersusDealDto)deal;
        return VersusDealEntity.Create(c.Guarantee, c.ArtistDoorPercent, c.PaymentMethod).Map<DealEntity>(entity => entity);
    }
}
