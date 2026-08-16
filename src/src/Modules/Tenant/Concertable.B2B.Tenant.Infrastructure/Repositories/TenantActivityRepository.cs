using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class TenantActivityRepository : Repository<TenantActivityEntity>, ITenantActivityRepository
{
    private readonly TenantDbContext tenantContext;

    public TenantActivityRepository(TenantDbContext context) : base(context)
    {
        tenantContext = context;
    }

    public Task<bool> ExistsAsync(Guid tenantId, string sourceKey, CancellationToken ct = default) =>
        tenantContext.Activities.AnyAsync(
            activity => activity.TenantId == tenantId && activity.SourceKey == sourceKey,
            ct);

    public async Task<IReadOnlyList<TenantActivityEntity>> GetRecentAsync(
        Guid tenantId,
        int take,
        CancellationToken ct = default) =>
        await tenantContext.Activities
            .AsNoTracking()
            .Where(activity => activity.TenantId == tenantId)
            .OrderByDescending(activity => activity.At)
            .ThenByDescending(activity => activity.Id)
            .Take(take)
            .ToListAsync(ct);
}
