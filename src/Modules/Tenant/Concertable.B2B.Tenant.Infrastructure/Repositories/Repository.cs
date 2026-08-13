using Concertable.B2B.Tenant.Infrastructure.Data;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal abstract class WriteRepository<TEntity>(TenantDbContext context)
    : WriteRepository<TEntity, TenantDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(TenantDbContext context)
    : Repository<TEntity, TenantDbContext, Guid>(context)
    where TEntity : class, IGuidEntity;
