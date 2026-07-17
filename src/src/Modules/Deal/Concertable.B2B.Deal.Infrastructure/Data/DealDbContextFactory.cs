using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Data;

internal sealed class DealDbContextFactory : B2BDesignTimeDbContextFactory<DealDbContext>
{
    protected override DealDbContext Create(DbContextOptions<DealDbContext> options) =>
        new(options, new DealConfigurationProvider());
}
