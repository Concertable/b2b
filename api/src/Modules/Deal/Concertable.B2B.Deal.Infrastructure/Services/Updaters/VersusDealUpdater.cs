using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class VersusDealUpdater : IDealUpdater
{
    public UnitResult<ValidationErrors> Apply(DealEntity existing, DealDto source)
    {
        var entity = (VersusDealEntity)existing;
        var deal = (VersusDealDto)source;
        return entity.Update(deal.Guarantee, deal.ArtistDoorPercent, deal.PaymentMethod);
    }
}
