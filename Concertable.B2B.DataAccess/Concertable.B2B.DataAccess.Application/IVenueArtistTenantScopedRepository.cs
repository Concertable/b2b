using Concertable.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// Repository over a two-party (<see cref="IVenueArtistTenantScoped"/>) entity — e.g. an application,
/// booking or concert. The single-owner counterpart is <see cref="ITenantScopedRepository{TEntity, TKey}"/>.
/// </summary>
public interface IVenueArtistTenantScopedRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IVenueArtistTenantScoped
{
    /// <summary>Both party ids off a single row — the snapshot settlement and checkout read. Scalar, no row load.</summary>
    Task<(Guid VenueTenantId, Guid ArtistTenantId)?> GetTenantPairAsync(TKey id, CancellationToken ct = default);

    /// <summary>The venue-side tenant of a single row. Scalar, no row load.</summary>
    Task<Guid?> GetVenueTenantIdAsync(TKey id, CancellationToken ct = default);

    /// <summary>The artist-side tenant of a single row. Scalar, no row load.</summary>
    Task<Guid?> GetArtistTenantIdAsync(TKey id, CancellationToken ct = default);
}

public interface IVenueArtistTenantScopedRepository<TEntity> : IVenueArtistTenantScopedRepository<TEntity, int>
    where TEntity : class, IIdEntity, IVenueArtistTenantScoped;
