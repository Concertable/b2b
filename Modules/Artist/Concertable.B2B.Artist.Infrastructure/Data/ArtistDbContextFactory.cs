using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class ArtistDbContextFactory : IDesignTimeDbContextFactory<ArtistDbContext>
{
    public ArtistDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__B2BDb")
            ?? "Server=localhost,1433;Database=concertable-b2b;User Id=sa;Password=Password11!;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ArtistDbContext>()
            .UseSqlServer(connectionString, o => o.UseNetTopologySuite())
            .Options;
        return new ArtistDbContext(options, new ArtistConfigurationProvider());
    }
}
