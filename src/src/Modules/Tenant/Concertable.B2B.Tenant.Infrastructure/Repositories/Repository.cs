using Concertable.B2B.Tenant.Infrastructure.Data;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal abstract class Repository<TEntity>(TenantDbContext context)
    : Repository<TEntity, Guid>(context)
    where TEntity : class, IGuidEntity;
