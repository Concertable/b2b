using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class TenantScopedRepository<TEntity, TKey>
    : Repository<TEntity, TKey>, ITenantScopedRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, ITenantScoped
{
    private readonly ITenantContext tenant;

    protected TenantScopedRepository(IDbContext context, ITenantContext tenant) : base(context)
    {
        this.tenant = tenant;
    }

    /// <summary>The entity set scoped to the current request's tenant — build tenant-scoped reads off this.</summary>
    protected IQueryable<TEntity> CurrentTenant =>
        base.Context.Query<TEntity>().Where(e => (Guid?)e.TenantId == tenant.TenantId);

    public async Task<Guid?> GetTenantIdByIdAsync(TKey id, CancellationToken ct = default) =>
        await base.Context.Query<TEntity>()
            .Where(e => e.Id!.Equals(id))
            .Select(e => (Guid?)e.TenantId)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TEntity>> GetAllByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        await base.Context.Query<TEntity>().Where(e => e.TenantId == tenantId).ToListAsync(ct);
}
