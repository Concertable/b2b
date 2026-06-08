using Concertable.DataAccess.Infrastructure;
using Concertable.B2B.User.Infrastructure.Data;

namespace Concertable.B2B.User.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(UserDbContext context)
    : BaseRepository<TEntity, UserDbContext>(context)
    where TEntity : class;
