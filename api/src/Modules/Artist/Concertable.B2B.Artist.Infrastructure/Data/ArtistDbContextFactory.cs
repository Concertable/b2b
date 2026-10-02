using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class ArtistDbContextFactory : B2BDesignTimeDbContextFactory<ArtistDbContext>
{
    protected override string MigrationsSchema => Schema.Name;

    protected override ArtistDbContext Create(DbContextOptions<ArtistDbContext> options) =>
        new(options, DefaultOutboxOptions, new ArtistConfigurationProvider(), DesignTimeTenantContext.Instance);

    protected override void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.UseNetTopologySuite();
}
