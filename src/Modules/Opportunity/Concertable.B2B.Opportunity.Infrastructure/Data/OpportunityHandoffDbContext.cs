using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Data;

internal sealed class OpportunityHandoffDbContext(
    DbContextOptions<OpportunityHandoffDbContext> options,
    OpportunityConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
}
