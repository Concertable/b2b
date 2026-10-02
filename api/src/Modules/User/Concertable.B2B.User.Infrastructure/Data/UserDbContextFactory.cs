using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Concertable.B2B.User.Infrastructure.Data;

internal sealed class UserDbContextFactory : B2BDesignTimeDbContextFactory<UserDbContext>
{
    protected override string MigrationsSchema => Schema.Name;

    protected override UserDbContext Create(DbContextOptions<UserDbContext> options) =>
        new(options, DefaultOutboxOptions, new UserConfigurationProvider());

    protected override void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.UseNetTopologySuite();
}
