using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertDbContextFactory : IDesignTimeDbContextFactory<ConcertDbContext>
{
    public ConcertDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__B2BDb")
            ?? "Server=localhost,1433;Database=concertable-b2b;User Id=sa;Password=Password11!;TrustServerCertificate=True";
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
