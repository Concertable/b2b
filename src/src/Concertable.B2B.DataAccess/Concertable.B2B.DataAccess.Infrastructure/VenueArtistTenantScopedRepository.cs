using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class VenueArtistTenantScopedRepository<TEntity, TContext, TKey>
    : Repository<TEntity, TContext, TKey>, IVenueArtistTenantScopedRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IVenueArtistTenantScoped
    where TContext : DbContextBase
{
    protected VenueArtistTenantScopedRepository(TContext context) : base(context) { }

    public async Task<(Guid VenueTenantId, Guid ArtistTenantId)?> GetTenantPairAsync(TKey id, CancellationToken ct = default)
    {
        var pair = await context.Set<TEntity>()
            .Where(e => e.Id!.Equals(id))
            .Select(e => new { e.VenueTenantId, e.ArtistTenantId })
            .FirstOrDefaultAsync(ct);
        return pair is null ? null : (pair.VenueTenantId, pair.ArtistTenantId);
    }

    public async Task<Guid?> GetVenueTenantIdAsync(TKey id, CancellationToken ct = default) =>
        await context.Set<TEntity>()
            .Where(e => e.Id!.Equals(id))
            .Select(e => (Guid?)e.VenueTenantId)
            .FirstOrDefaultAsync(ct);

    public async Task<Guid?> GetArtistTenantIdAsync(TKey id, CancellationToken ct = default) =>
        await context.Set<TEntity>()
            .Where(e => e.Id!.Equals(id))
            .Select(e => (Guid?)e.ArtistTenantId)
            .FirstOrDefaultAsync(ct);
}
