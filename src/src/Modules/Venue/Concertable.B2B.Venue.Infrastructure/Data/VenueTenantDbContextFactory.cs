using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenueTenantDbContextFactory : B2BDesignTimeDbContextFactory<VenueTenantDbContext>
{
    protected override VenueTenantDbContext Create(DbContextOptions<VenueTenantDbContext> options) =>
        new(options, new VenueConfigurationProvider(), DesignTimeTenantContext.Instance);

    protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql) =>
        sql.UseNetTopologySuite();
}
