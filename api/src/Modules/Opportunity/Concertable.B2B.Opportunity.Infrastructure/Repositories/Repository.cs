using Concertable.B2B.Opportunity.Infrastructure.Data;

namespace Concertable.B2B.Opportunity.Infrastructure.Repositories;

internal abstract class Repository<TEntity>(OpportunityDbContext context)
    : Repository<TEntity, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(OpportunityDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;
