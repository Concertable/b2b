using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.B2B.User.Infrastructure.Data;

namespace Concertable.B2B.User.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(UserDbContext context)
    : BaseRepository<TEntity, UserDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(UserDbContext context)
    : Repository<TEntity, UserDbContext>(context)
    where TEntity : class, IIdEntity;

internal abstract class GuidRepository<TEntity>(UserDbContext context)
    : GuidRepository<TEntity, UserDbContext>(context)
    where TEntity : class, IGuidEntity;
