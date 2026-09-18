using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Data;

/// <summary>The unfiltered, unfenced stance for work no human is acting in. See <see cref="OpportunityDbContext"/>.</summary>
internal sealed class OpportunityPrivilegedDbContext(
    DbContextOptions<OpportunityPrivilegedDbContext> options,
    OpportunityConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
}
