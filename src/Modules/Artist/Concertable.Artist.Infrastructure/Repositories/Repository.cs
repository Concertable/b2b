using Concertable.Artist.Infrastructure.Data;

namespace Concertable.Artist.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(ArtistDbContext context)
    : BaseRepository<TEntity, ArtistDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(ArtistDbContext context)
    : Repository<TEntity, ArtistDbContext>(context)
    where TEntity : class, IIdEntity;

internal abstract class GuidRepository<TEntity>(ArtistDbContext context)
    : GuidRepository<TEntity, ArtistDbContext>(context)
    where TEntity : class, IGuidEntity;
