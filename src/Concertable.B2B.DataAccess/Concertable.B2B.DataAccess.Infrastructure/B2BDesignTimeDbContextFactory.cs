using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class B2BDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(DesignTimeConfiguration.ConnectionString(), ConfigureSqlServer)
            .Options;
        return Create(options);
    }

    protected abstract TContext Create(DbContextOptions<TContext> options);

    protected virtual void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql)
    {
    }
}
