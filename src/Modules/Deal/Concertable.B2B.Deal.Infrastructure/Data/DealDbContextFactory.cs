using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.B2B.Deal.Infrastructure.Data;

internal sealed class DealDbContextFactory : IDesignTimeDbContextFactory<DealDbContext>
{
    public DealDbContext CreateDbContext(string[] args)
    {
        var connectionString = DesignTimeConnectionString.B2B();
        var options = new DbContextOptionsBuilder<DealDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new DealDbContext(options, new DealConfigurationProvider());
    }
}
