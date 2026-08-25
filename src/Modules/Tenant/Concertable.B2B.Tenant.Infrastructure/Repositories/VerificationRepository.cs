using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class VerificationRepository : Repository<TenantVerificationEntity>, IVerificationRepository
{
    private readonly TenantDbContext context;

    public VerificationRepository(TenantDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<TenantVerificationEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Verifications
            .Include(v => v.Documents)
            .FirstOrDefaultAsync(v => v.TenantId == tenantId, ct);
}
