using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Data;

internal sealed class OpportunityDbContext(
    DbContextOptions<OpportunityDbContext> options,
    OpportunityConfigurationProvider provider,
    ITenantContext tenantContext)
    : TenantScopedDbContext(options, provider, tenantContext, Schema.Name)
{
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder) =>
        modelBuilder.ApplySingleOwner<OpportunityEntity>(this);
}
