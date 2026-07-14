using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Application.Mappers;

internal sealed class DoorSplitDealMapper : IDealMapper
{
    public IDeal ToDeal(DealEntity entity)
    {
        var e = (DoorSplitDealEntity)entity;
        return new DoorSplitDeal
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            ArtistDoorPercent = e.ArtistDoorPercent
        };
    }

    public DealEntity ToEntity(IDeal contract)
    {
        var c = (DoorSplitDeal)contract;
        return DoorSplitDealEntity.Create(c.ArtistDoorPercent, c.PaymentMethod);
    }
}
