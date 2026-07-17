using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertDbContextFactory : IDesignTimeDbContextFactory<ConcertDbContext>
{
    public ConcertDbContext CreateDbContext(string[] args)
    {
        var connectionString = DesignTimeConnectionString.B2B();
        var options = new DbContextOptionsBuilder<ConcertDbContext>()
            .UseSqlServer(connectionString, o => o.UseNetTopologySuite())
            .Options;
        return new ConcertDbContext(options, new ConcertConfigurationProvider(), new DesignTimeTenantContext());
    }

    /* Design-time only builds the model; no query ever evaluates the filter. */
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public bool IsHost => true;
    }
}
