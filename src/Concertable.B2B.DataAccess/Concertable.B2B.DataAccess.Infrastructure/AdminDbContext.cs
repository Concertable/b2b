using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The platform-admin stance of a module's data: composes the module's own anemic configuration provider
/// with no tenancy on top — writable, so a cross-tenant operator (e.g. venue approval) can act on rows it
/// does not own; the tenant write-guard interceptor no-ops for a tenant-less admin. One concrete subclass
/// per module that has admin operations (e.g. <c>AdminVenueDbContext</c>), preserving module isolation.
/// The unfiltered read-only counterpart is <see cref="PublicDbContext"/>; the tenant-filtered, writable
/// one is <see cref="TenantScopedDbContext"/>.
/// </summary>
public abstract class AdminDbContext : DbContextBase
{
    private readonly IEntityTypeConfigurationProvider provider;
    private readonly string defaultSchema;

    protected AdminDbContext(DbContextOptions options, IEntityTypeConfigurationProvider provider, string defaultSchema)
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
}
