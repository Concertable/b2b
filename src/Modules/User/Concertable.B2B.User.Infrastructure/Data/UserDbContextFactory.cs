using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.B2B.User.Infrastructure.Data;

internal sealed class UserDbContextFactory : B2BDesignTimeDbContextFactory<UserDbContext>
{
    protected override UserDbContext Create(DbContextOptions<UserDbContext> options) =>
        new(options, new UserConfigurationProvider());

    protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql) =>
        sql.UseNetTopologySuite();
}
