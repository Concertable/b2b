using Concertable.B2B.Contract.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Contract.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(ContractDbContext context)
    : BaseRepository<TEntity, ContractDbContext>(context)
    where TEntity : class;

internal abstract class ReadRepository<TEntity>(ContractDbContext context)
    : ReadRepository<TEntity, ContractDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class Repository<TEntity>(ContractDbContext context)
    : Repository<TEntity, ContractDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(ContractDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, ContractDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;
