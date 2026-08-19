using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.B2B.Admin.Infrastructure.Data;

namespace Concertable.B2B.Admin.Infrastructure.Repositories;

internal abstract class Repository<TEntity>(AdminDbContext context)
    : Repository<TEntity, Guid>(context)
    where TEntity : class, IGuidEntity;
