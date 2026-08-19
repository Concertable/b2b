using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Admin.Infrastructure.Data;

internal sealed class AdminDbContextFactory : B2BDesignTimeDbContextFactory<AdminDbContext>
{
    protected override AdminDbContext Create(DbContextOptions<AdminDbContext> options) =>
        new(options, new AdminConfigurationProvider());
}
