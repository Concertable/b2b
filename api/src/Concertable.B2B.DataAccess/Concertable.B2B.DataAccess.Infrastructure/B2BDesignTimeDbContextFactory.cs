using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class B2BDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    protected static IOptions<OutboxOptions> DefaultOutboxOptions { get; } = Options.Create(new OutboxOptions());

    public TContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(DesignTimeConfiguration.ConnectionString(), ConfigureSqlServer)
            .Options;
        return Create(options);
    }

    protected abstract TContext Create(DbContextOptions<TContext> options);

    protected virtual void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql) { }
}
