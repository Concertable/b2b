using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertTenantDbContextFactory : B2BDesignTimeDbContextFactory<ConcertTenantDbContext>
{
    protected override ConcertTenantDbContext Create(DbContextOptions<ConcertTenantDbContext> options) =>
        new(options, new ConcertConfigurationProvider(), DesignTimeTenantContext.Instance);

    protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql) =>
        sql.UseNetTopologySuite();
}
