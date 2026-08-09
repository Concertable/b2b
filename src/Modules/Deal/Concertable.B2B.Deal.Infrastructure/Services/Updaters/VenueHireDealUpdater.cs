using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class VenueHireDealUpdater : IDealUpdater
{
    public UnitResult<ValidationErrors> Apply(DealEntity existing, IDeal source)
    {
        var entity = (VenueHireDealEntity)existing;
        var deal = (VenueHireDeal)source;
        return entity.Update(deal.HireFee, deal.PaymentMethod);
    }
}
