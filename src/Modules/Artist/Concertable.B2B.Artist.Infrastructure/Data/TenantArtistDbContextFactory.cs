using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class TenantArtistDbContextFactory : B2BDesignTimeDbContextFactory<TenantArtistDbContext>
{
    protected override TenantArtistDbContext Create(DbContextOptions<TenantArtistDbContext> options) =>
        new(options, new ArtistConfigurationProvider(), DesignTimeTenantContext.Instance);

    protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql) =>
        sql.UseNetTopologySuite();
}
