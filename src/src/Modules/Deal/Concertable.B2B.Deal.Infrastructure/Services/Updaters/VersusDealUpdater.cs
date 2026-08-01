using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class VersusDealUpdater : IDealUpdater
{
    public UnitResult<ValidationErrors> Apply(DealEntity existing, IDeal source)
    {
        var entity = (VersusDealEntity)existing;
        var deal = (VersusDeal)source;
        return entity.Update(deal.Guarantee, deal.ArtistDoorPercent, deal.PaymentMethod);
    }
}
