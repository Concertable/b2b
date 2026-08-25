using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Opportunity.Domain.Entities;

namespace Concertable.B2B.Opportunity.Application.Mappers;

internal static class OpportunityMappers
{
    extension(OpportunityEntity opportunity)
    {
        public OpportunityDto ToDto() => new(
            opportunity.Id,
            opportunity.VenueId,
            opportunity.TenantId,
            opportunity.DealId,
            opportunity.Period.Start,
            opportunity.Period.End,
            opportunity.Genres.ToHashSet());
    }
}
