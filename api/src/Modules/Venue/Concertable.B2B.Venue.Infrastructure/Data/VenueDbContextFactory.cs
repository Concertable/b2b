using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenueDbContextFactory : B2BDesignTimeDbContextFactory<VenueDbContext>
{
    protected override string MigrationsSchema => Schema.Name;

    protected override VenueDbContext Create(DbContextOptions<VenueDbContext> options) =>
        new(options, DefaultOutboxOptions, new VenueConfigurationProvider(), DesignTimeTenantContext.Instance);

    protected override void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.UseNetTopologySuite();
}
