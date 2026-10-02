using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Privacy.Infrastructure.Data;

internal sealed class PrivacyDbContext : DbContextBase
{
    private readonly PrivacyConfigurationProvider provider;

    public PrivacyDbContext(
        DbContextOptions<PrivacyDbContext> options,
        IOptions<OutboxOptions> outboxOptions,
        PrivacyConfigurationProvider provider)
        : base(options, outboxOptions)
    {
        this.provider = provider;
    }

    public DbSet<SubjectErasureRequestEntity> SubjectErasureRequests => base.Set<SubjectErasureRequestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        this.provider.Configure(modelBuilder);
    }
}
