using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;
using Concertable.Kernel;
using static Concertable.Seed.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class OpportunityFactory
{
    public static OpportunityEntity Create(int id, int venueId, DateRange period, int contractId, IEnumerable<Genre>? genres = null)
        => Create(venueId, period, contractId, genres).With(nameof(OpportunityEntity.Id), id);

    public static OpportunityEntity Create(int venueId, DateRange period, int contractId, IEnumerable<Genre>? genres = null)
    {
        var opp = New<OpportunityEntity>()
            .With(nameof(OpportunityEntity.VenueId), venueId)
            .With(nameof(OpportunityEntity.Period), period)
            .With(nameof(OpportunityEntity.ContractId), contractId);
        if (genres is not null)
            opp.With(nameof(OpportunityEntity.Genres), genres.ToList());
        return opp;
    }
}
