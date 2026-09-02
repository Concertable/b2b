using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts.Enums;
using Concertable.Kernel.ValueObjects;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class OpportunityFactory
{
    public static OpportunityEntity Create(int id, int venueId, DateRange period, int dealId)
        => OpportunityEntity.Create(venueId, period, dealId).WithId(id);

    public static OpportunityEntity Create(int id, int venueId, DateRange period, int dealId, IReadOnlyCollection<Genre> genres)
        => OpportunityEntity.Create(venueId, period, dealId, genres).WithId(id);
}
