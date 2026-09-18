using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class B2BDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    protected static IOptions<OutboxOptions> DefaultOutboxOptions { get; } =
        Options.Create(new OutboxOptions());

    public TContext CreateDbContext(string[] args) =>
        CreateDbContext(DesignTimeConfiguration.ConnectionString());

    public TContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MigrationsSchema);
                ConfigureNpgsql(npgsql);
            })
            .Options;
        return Create(options);
    }

    protected abstract TContext Create(DbContextOptions<TContext> options);

    protected abstract string MigrationsSchema { get; }

    protected virtual void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) { }
}
