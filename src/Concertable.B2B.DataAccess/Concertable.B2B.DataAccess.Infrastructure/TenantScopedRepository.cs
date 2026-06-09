using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class TenantScopedRepository<TEntity, TContext, TKey>
    : Repository<TEntity, TContext, TKey>, ITenantScopedRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, ITenantScoped
    where TContext : DbContextBase
{
    private readonly ITenantContext tenant;

    protected TenantScopedRepository(TContext context, ITenantContext tenant) : base(context)
    {
        this.tenant = tenant;
    }

    /// <summary>The entity set scoped to the current request's tenant — build tenant-scoped reads off this.</summary>
    protected IQueryable<TEntity> CurrentTenant =>
        context.Set<TEntity>().Where(e => (Guid?)e.TenantId == tenant.TenantId);

    public async Task<IReadOnlyList<TEntity>> GetAllByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        await context.Set<TEntity>().Where(e => e.TenantId == tenantId).ToListAsync(ct);
}
