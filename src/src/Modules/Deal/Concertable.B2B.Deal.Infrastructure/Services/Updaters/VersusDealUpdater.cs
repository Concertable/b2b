using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class VersusDealUpdater : IDealUpdater
{
    public void Apply(DealEntity existing, IDeal source)
    {
        var entity = (VersusDealEntity)existing;
        var deal = (VersusDeal)source;
        entity.Update(deal.Guarantee, deal.ArtistDoorPercent, deal.PaymentMethod);
    }
}
