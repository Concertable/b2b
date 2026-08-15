using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantActivityService : ITenantActivityService
{
    private readonly TenantDbContext context;

    public TenantActivityService(TenantDbContext context)
    {
        this.context = context;
    }

    public async Task AddAsync(ActivityRecord record, CancellationToken ct = default)
    {
        if (await context.Activities.AnyAsync(
                a => a.TenantId == record.TenantId && a.SourceKey == record.SourceKey,
                ct))
            return;

        context.Activities.Add(TenantActivityEntity.Create(record));
    }

    public async Task<IReadOnlyList<ActivityItemDto>> GetRecentAsync(
        Guid tenantId,
        int take,
        CancellationToken ct = default) =>
        await context.Activities
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.At)
            .ThenByDescending(a => a.Id)
            .Take(take)
            .Select(a => new ActivityItemDto(
                a.Id,
                a.Type,
                a.At,
                a.Subject,
                a.Detail,
                a.Url))
            .ToListAsync(ct);
}
