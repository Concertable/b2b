using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class TenantRepository : Repository<TenantEntity>, ITenantRepository
{
    public TenantRepository(TenantDbContext context) : base(context) { }

    public Task<TenantMembershipEntity?> GetMembershipByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        context.Memberships.FirstOrDefaultAsync(m => m.UserId == userId, ct);
}
