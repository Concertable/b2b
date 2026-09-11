using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Data;

internal sealed class OpportunityDbContextFactory : B2BDesignTimeDbContextFactory<OpportunityDbContext>
{
    protected override OpportunityDbContext Create(DbContextOptions<OpportunityDbContext> options) =>
        new(options, new OpportunityConfigurationProvider(), DesignTimeTenantContext.Instance);
}
