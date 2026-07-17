using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class TenantDbContextFactory : B2BDesignTimeDbContextFactory<TenantDbContext>
{
    protected override TenantDbContext Create(DbContextOptions<TenantDbContext> options) =>
        new(options, new TenantConfigurationProvider());
}
