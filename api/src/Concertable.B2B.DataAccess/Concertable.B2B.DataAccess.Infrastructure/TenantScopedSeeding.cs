using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

public static class TenantScopedSeeding
{
    extension(DbContext context)
    {
        /// <summary>
        /// Saves tenant-owned seed rows one tenant at a time, acting as each in turn. The write guard
        /// demands a current tenant that matches the row, and a seeder holds rows for every tenant at
        /// once, so a single save can never satisfy it.
        /// </summary>
        public async Task SeedByTenantAsync<TEntity>(
            ITenantScope scope,
            IEnumerable<TEntity> rows,
            CancellationToken ct = default)
            where TEntity : class, ITenantScoped
        {
            foreach (var owned in rows.GroupBy(row => row.TenantId))
            {
                using var acting = scope.As(owned.Key);
                context.Set<TEntity>().AddRange(owned);
                await context.SaveChangesAsync(ct);
            }
        }
    }
}
