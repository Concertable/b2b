using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertDbContextFactory : B2BDesignTimeDbContextFactory<ConcertDbContext>
{
    protected override string MigrationsSchema => Schema.Name;

    protected override ConcertDbContext Create(DbContextOptions<ConcertDbContext> options) =>
        new(options, DefaultOutboxOptions, new ConcertConfigurationProvider(), DesignTimeTenantContext.Instance);

    protected override void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.UseNetTopologySuite();
}
