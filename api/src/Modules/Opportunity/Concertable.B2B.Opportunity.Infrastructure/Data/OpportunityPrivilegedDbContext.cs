using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Opportunity.Infrastructure.Data;

internal sealed class OpportunityPrivilegedDbContext(
    DbContextOptions<OpportunityPrivilegedDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    OpportunityConfigurationProvider provider)
    : PrivilegedDbContext(options, outboxOptions, provider, Schema.Name)
{
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
}
