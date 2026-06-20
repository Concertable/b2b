using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class TenantRepository : Repository<TenantEntity>, ITenantRepository
{
    public TenantRepository(TenantDbContext context) : base(context) { }

    public Task<ActiveMembership?> GetActiveMembershipAsync(Guid userId, CancellationToken ct = default) =>
        context.Memberships
            .Where(m => m.UserId == userId)
            .Join(
                context.Tenants,
                m => m.TenantId,
                t => t.Id,
                (m, t) => new ActiveMembership(m.TenantId, m.Role, t.Type))
            .FirstOrDefaultAsync(ct);
}
