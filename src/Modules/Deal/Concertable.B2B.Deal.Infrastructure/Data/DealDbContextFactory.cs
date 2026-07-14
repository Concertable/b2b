using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.B2B.Deal.Infrastructure.Data;

internal sealed class DealDbContextFactory : IDesignTimeDbContextFactory<DealDbContext>
{
    public DealDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__B2BDb")
            ?? "Server=localhost,1433;Database=concertable-b2b;User Id=sa;Password=Password11!;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<DealDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new DealDbContext(options, new DealConfigurationProvider());
    }
}
