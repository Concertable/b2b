using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Opportunity.Infrastructure.Data;

internal sealed class OpportunityDbContext(
    DbContextOptions<OpportunityDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    OpportunityConfigurationProvider provider,
    ITenantContext tenantContext)
    : TenantScopedDbContext(options, outboxOptions, provider, tenantContext, Schema.Name)
{
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder) =>
        modelBuilder.ApplySingleOwner<OpportunityEntity>(this);
}
