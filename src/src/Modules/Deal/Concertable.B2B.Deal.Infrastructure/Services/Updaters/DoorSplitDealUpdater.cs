using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class DoorSplitDealUpdater : IDealUpdater
{
    public UnitResult<ValidationErrors> Apply(DealEntity existing, IDeal source)
    {
        var entity = (DoorSplitDealEntity)existing;
        var deal = (DoorSplitDeal)source;
        return entity.Update(deal.ArtistDoorPercent, deal.PaymentMethod);
    }
}
