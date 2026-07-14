using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class DoorSplitDealUpdater : IDealUpdater
{
    public void Apply(DealEntity existing, IDeal source)
    {
        var entity = (DoorSplitDealEntity)existing;
        var contract = (DoorSplitDeal)source;
        entity.Update(contract.ArtistDoorPercent, contract.PaymentMethod);
    }
}
