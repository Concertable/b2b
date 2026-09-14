using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class DoorSplitDealUpdater : IDealUpdater
{
    public UnitResult<ValidationErrors> Apply(DealEntity existing, DealDto source)
    {
        var entity = (DoorSplitDealEntity)existing;
        var deal = (DoorSplitDealDto)source;
        return entity.Update(deal.ArtistDoorPercent, deal.PaymentMethod);
    }
}
