using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class ArtistDbContextFactory : IDesignTimeDbContextFactory<ArtistDbContext>
{
    public ArtistDbContext CreateDbContext(string[] args)
    {
        var connectionString = DesignTimeConnectionString.B2B();
        var options = new DbContextOptionsBuilder<ArtistDbContext>()
            .UseSqlServer(connectionString, o => o.UseNetTopologySuite())
            .Options;
        return new ArtistDbContext(options, new ArtistConfigurationProvider(), new DesignTimeTenantContext());
    }

    /* Design-time only builds the model; no query ever evaluates the filter. */
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public bool IsHost => true;
    }
}
