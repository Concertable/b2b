using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.B2B.Privacy.Infrastructure.Data;

namespace Concertable.B2B.Privacy.Infrastructure.Repositories;

internal abstract class Repository<TEntity>(PrivacyDbContext context)
    : Repository<TEntity, Guid>(context)
    where TEntity : class, IGuidEntity;
