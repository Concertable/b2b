using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.B2B.User.Infrastructure.Data;

namespace Concertable.B2B.User.Infrastructure.Repositories;

internal abstract class Repository<TEntity>(UserDbContext context)
    : Repository<TEntity, Guid>(context)
    where TEntity : class, IGuidEntity;
