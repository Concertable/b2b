using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class TenantConcertDbContextFactory : B2BDesignTimeDbContextFactory<TenantConcertDbContext>
{
    protected override TenantConcertDbContext Create(DbContextOptions<TenantConcertDbContext> options) =>
        new(options, new ConcertConfigurationProvider(), DesignTimeTenantContext.Instance);

    protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql) =>
        sql.UseNetTopologySuite();
}
