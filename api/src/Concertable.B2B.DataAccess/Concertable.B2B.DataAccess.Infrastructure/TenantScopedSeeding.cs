using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// Seeds a tenant-filtered context one tenant at a time. A seeder has no request to resolve a tenant from and
/// holds rows for every tenant at once, so neither half of the stance is satisfiable in a single pass: the
/// write guard refuses a save whose rows do not all belong to the current tenant, and the query filter hides
/// every row from the seed-if-empty read that decides whether to write at all. Acting as each owning tenant in
/// turn answers both through the module's own context, which is where the pre-commit domain-event handlers
/// read — a second seeding context leaves them with nothing.
/// </summary>
public static class TenantScopedSeeding
{
    extension(DbContext context)
    {
        /// <summary>Seeds single-owner rows, acting as each owning tenant in turn.</summary>
        public async Task SeedByTenantAsync<TEntity>(
            ITenantScope scope,
            IEnumerable<TEntity> rows,
            CancellationToken ct = default)
            where TEntity : class, ITenantScoped
        {
            foreach (var owned in rows.GroupBy(row => row.TenantId))
            {
                using var acting = scope.As(owned.Key);

                if (await context.Set<TEntity>().AnyAsync(row => row.TenantId == owned.Key, ct))
                    continue;

                context.Set<TEntity>().AddRange(owned);
                await context.SaveChangesAsync(ct);
            }
        }

        /// <summary>
        /// Seeds two-party rows, acting as each venue-side tenant in turn. The guard re-states the venue side
        /// rather than trusting the filter alone, which also matches the rows a tenant merely holds the artist
        /// side of and would skip a venue whose own rows are still absent.
        /// </summary>
        public async Task SeedByVenueTenantAsync<TEntity>(
            ITenantScope scope,
            IEnumerable<TEntity> rows,
            CancellationToken ct = default)
            where TEntity : class, IVenueArtistTenantScoped
        {
            foreach (var owned in rows.GroupBy(row => row.VenueTenantId))
            {
                using var acting = scope.As(owned.Key);

                if (await context.Set<TEntity>().AnyAsync(row => row.VenueTenantId == owned.Key, ct))
                    continue;

                context.Set<TEntity>().AddRange(owned);
                await context.SaveChangesAsync(ct);
            }
        }
    }
}
