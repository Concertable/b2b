using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The public stance of a module's data: composes the module's own anemic configuration provider
/// with no tenancy on top — public by construction, nothing is lifted because nothing was applied.
/// One concrete subclass per module (e.g. <c>PublicConcertDbContext</c>), preserving module
/// isolation: a module's public reads see only that module's model. Read-only by construction —
/// writes throw, so the write-side tenant guard on the module's real context can never be bypassed
/// here. The tenant-filtered counterpart is <see cref="VenueArtistTenantDbContext"/>.
/// </summary>
public abstract class PublicDbContext : DbContextBase
{
    private readonly IEntityTypeConfigurationProvider provider;
    private readonly string defaultSchema;

    protected PublicDbContext(DbContextOptions options, IEntityTypeConfigurationProvider provider, string defaultSchema)
        : base(options)
    {
        this.provider = provider;
        this.defaultSchema = defaultSchema;
    }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(defaultSchema);
        provider.Configure(modelBuilder);
    }

    public sealed override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException("Public contexts are read-only.");

    public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Public contexts are read-only.");
}
