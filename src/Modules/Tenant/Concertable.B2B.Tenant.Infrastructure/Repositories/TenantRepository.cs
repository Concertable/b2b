using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class TenantRepository : Repository<TenantEntity>, ITenantRepository
{
    public TenantRepository(TenantDbContext context) : base(context) { }

    public Task<TenantEntity?> GetByCreatedByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        context.Tenants.FirstOrDefaultAsync(t => t.CreatedByUserId == userId, ct);
}
