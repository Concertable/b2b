using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal sealed class OpportunitySpecification : SpecificationBuilder<OpportunityEntity>
{
    public static ISpecification<OpportunityEntity> CreateWithVenue() =>
        new OpportunitySpecification().Include(opportunity => opportunity.Venue);

    public static ISpecification<OpportunityEntity, int?> CreateDealId() =>
        new OpportunitySpecification().Select(opportunity => opportunity.DealId);
}
