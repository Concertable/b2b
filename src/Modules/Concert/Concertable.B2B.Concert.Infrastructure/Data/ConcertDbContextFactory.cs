using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertDbContextFactory : B2BDesignTimeDbContextFactory<ConcertDbContext>
{
    protected override ConcertDbContext Create(DbContextOptions<ConcertDbContext> options) =>
        new(options, new ConcertConfigurationProvider(), DesignTimeTenantContext.Instance);

    protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql) =>
        sql.UseNetTopologySuite();
}
