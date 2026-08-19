using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Admin.Infrastructure.Data;

internal sealed class AdminProvisioningDbContextFactory : B2BDesignTimeDbContextFactory<AdminProvisioningDbContext>
{
    protected override AdminProvisioningDbContext Create(DbContextOptions<AdminProvisioningDbContext> options) =>
        new(options, new AdminConfigurationProvider());
}
