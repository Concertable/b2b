using Concertable.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// Repository over a tenant-owned (<see cref="ITenantScoped"/>) entity — e.g. a venue or opportunity,
/// <em>not</em> the tenant row itself. Inherited CRUD is unscoped (Bucket-A reads are public or explicitly
/// <c>UserId</c>-keyed today); tenant-scoped access is opt-in via the base's <c>CurrentTenant</c> query root,
/// and <see cref="GetAllByTenantIdAsync"/> reads a specific tenant's rows (admin / reporting). Write ownership
/// is enforced separately by the tenant SaveChanges interceptor.
/// </summary>
public interface ITenantScopedRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, ITenantScoped
{
    /// <summary>The owning tenant of a single row — scalar read, no row load.</summary>
    Task<Guid?> GetTenantIdByIdAsync(TKey id, CancellationToken ct = default);

    /// <summary>Every row owned by a specific tenant — for admin / cross-tenant reporting.</summary>
    Task<IReadOnlyList<TEntity>> GetAllByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}

public interface ITenantScopedRepository<TEntity> : ITenantScopedRepository<TEntity, int>
    where TEntity : class, IIdEntity, ITenantScoped;
