using Concertable.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// Repository over a two-party (<see cref="IVenueArtistTenantScoped"/>) entity — e.g. an application,
/// booking or concert. The single-owner counterpart is <see cref="ITenantScopedRepository{TEntity, TKey}"/>.
/// </summary>
public interface IVenueArtistTenantScopedRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IVenueArtistTenantScoped;

public interface IVenueArtistTenantScopedRepository<TEntity> : IVenueArtistTenantScopedRepository<TEntity, int>
    where TEntity : class, IIdEntity, IVenueArtistTenantScoped;
