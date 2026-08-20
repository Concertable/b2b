using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class VenueArtistTenantScopedRepository<TEntity, TKey>
    : Repository<TEntity, TKey>, IVenueArtistTenantScopedRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IVenueArtistTenantScoped
{
    protected VenueArtistTenantScopedRepository(IDbContext context) : base(context) { }

    public async Task<(Guid VenueTenantId, Guid ArtistTenantId)?> GetTenantPairAsync(TKey id, CancellationToken ct = default)
    {
        var pair = await base.Context.Query<TEntity>()
            .Where(e => e.Id!.Equals(id))
            .Select(e => new { e.VenueTenantId, e.ArtistTenantId })
            .FirstOrDefaultAsync(ct);
        return pair is null ? null : (pair.VenueTenantId, pair.ArtistTenantId);
    }

    public async Task<Guid?> GetVenueTenantIdAsync(TKey id, CancellationToken ct = default) =>
        await base.Context.Query<TEntity>()
            .Where(e => e.Id!.Equals(id))
            .Select(e => (Guid?)e.VenueTenantId)
            .FirstOrDefaultAsync(ct);

    public async Task<Guid?> GetArtistTenantIdAsync(TKey id, CancellationToken ct = default) =>
        await base.Context.Query<TEntity>()
            .Where(e => e.Id!.Equals(id))
            .Select(e => (Guid?)e.ArtistTenantId)
            .FirstOrDefaultAsync(ct);
}
