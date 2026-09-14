using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Data;

internal sealed class ApplicationDbContextFactory : B2BDesignTimeDbContextFactory<ApplicationDbContext>
{
    protected override ApplicationDbContext Create(DbContextOptions<ApplicationDbContext> options) =>
        new(options, new ApplicationConfigurationProvider(), DesignTimeTenantContext.Instance);
}
