using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Data;

/// <summary>
/// The Opportunity module's stance for work no human is acting in: the same mapping as
/// <see cref="OpportunityDbContext"/>, with neither the owning-tenant filter nor the tenant write fence. Only
/// seed factories and this module's own system work may inject it.
/// </summary>
internal sealed class OpportunityPrivilegedDbContext(
    DbContextOptions<OpportunityPrivilegedDbContext> options,
    OpportunityConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
}
