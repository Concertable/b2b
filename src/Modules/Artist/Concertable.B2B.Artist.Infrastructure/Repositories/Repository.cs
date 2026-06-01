using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.Kernel;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(ArtistDbContext context)
    : BaseRepository<TEntity, ArtistDbContext>(context)
    where TEntity : class;

internal abstract class ReadRepository<TEntity>(ArtistDbContext context)
    : ReadRepository<TEntity, ArtistDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class Repository<TEntity>(ArtistDbContext context)
    : Repository<TEntity, ArtistDbContext, int>(context)
    where TEntity : class, IIdEntity;
