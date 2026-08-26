using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class DoorSplitDealMapper : IDealMapper
{
    public DealDto ToDeal(DealEntity entity)
    {
        var e = (DoorSplitDealEntity)entity;
        return new DoorSplitDealDto
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            ArtistDoorPercent = e.ArtistDoorPercent
        };
    }

    public Result<DealEntity, ValidationErrors> ToEntity(DealDto deal)
    {
        var c = (DoorSplitDealDto)deal;
        return DoorSplitDealEntity.Create(c.ArtistDoorPercent, c.PaymentMethod).Map<DealEntity>(entity => entity);
    }
}
