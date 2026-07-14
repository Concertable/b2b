using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class FlatFeeDealUpdater : IDealUpdater
{
    public void Apply(DealEntity existing, IDeal source)
    {
        var entity = (FlatFeeDealEntity)existing;
        var deal = (FlatFeeDeal)source;
        entity.Update(deal.Fee, deal.PaymentMethod);
    }
}
