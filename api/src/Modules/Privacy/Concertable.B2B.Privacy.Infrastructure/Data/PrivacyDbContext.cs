using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Privacy.Infrastructure.Data;

/// <summary>The Privacy module's stance: unscoped and admin-operated (a DSAR is driven by a platform operator,
/// keyed by the subject's Auth <c>sub</c>, not by an ambient tenant), so it derives from <see cref="DbContextBase"/>
/// directly with no tenant filter.</summary>
internal sealed class PrivacyDbContext(
    DbContextOptions<PrivacyDbContext> options,
    PrivacyConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<SubjectErasureRequestEntity> SubjectErasureRequests => Set<SubjectErasureRequestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
