using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The unscoped-but-writable stance of a module's data: composes the module's own anemic configuration
/// provider with no tenancy on top — writable, so a cross-tenant operator (e.g. venue approval) can act
/// on rows it does not own; the tenant write-guard interceptor no-ops for a tenant-less write. One
/// concrete subclass per module that has admin operations (e.g. <c>VenuePrivilegedDbContext</c>), preserving
/// module isolation. The unfiltered read-only counterpart is <see cref="ReadDbContext"/>; the
/// tenant-filtered, writable one is <see cref="TenantScopedDbContext"/>.
/// </summary>
public abstract class PrivilegedDbContext : DbContextBase
{
    private readonly IEntityTypeConfigurationProvider provider;
    private readonly string defaultSchema;

    protected PrivilegedDbContext(DbContextOptions options, IEntityTypeConfigurationProvider provider, string defaultSchema)
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
