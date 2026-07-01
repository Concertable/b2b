using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;
using Concertable.Kernel;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class OpportunityFactory
{
    public static OpportunityEntity Create(int id, int venueId, DateRange period, int contractId, IEnumerable<Genre>? genres = null)
        => Create(venueId, period, contractId, genres).WithId(id);

    public static OpportunityEntity Create(int venueId, DateRange period, int contractId, IEnumerable<Genre>? genres = null)
        => OpportunityEntity.Create(venueId, period, contractId, genres);
}
