using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenueDbContextFactory : B2BDesignTimeDbContextFactory<VenueDbContext>
{
    protected override VenueDbContext Create(DbContextOptions<VenueDbContext> options) =>
        new(options, new VenueConfigurationProvider(), DesignTimeTenantContext.Instance);

    protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql) =>
        sql.UseNetTopologySuite();
}
