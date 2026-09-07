using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Data;

internal sealed class OpportunityReadDbContext(
    DbContextOptions<OpportunityReadDbContext> options,
    OpportunityConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name), IOpportunityReadDbContext
{
    IQueryable<OpportunityEntity> IOpportunityReadDbContext.Opportunities => Query<OpportunityEntity>();
}
