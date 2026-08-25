using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class VerificationRepository(TenantDbContext context)
    : Repository<TenantVerificationEntity>(context), IVerificationRepository
{
    public Task<TenantVerificationEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        Context.Query<TenantVerificationEntity>()
            .Include(v => v.Documents)
            .FirstOrDefaultAsync(v => v.TenantId == tenantId, ct);
}
