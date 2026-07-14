using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class VenueHireDealUpdater : IDealUpdater
{
    public void Apply(DealEntity existing, IDeal source)
    {
        var entity = (VenueHireDealEntity)existing;
        var contract = (VenueHireDeal)source;
        entity.Update(contract.HireFee, contract.PaymentMethod);
    }
}
