namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantActivityService : ITenantActivityService
{
    private readonly ITenantActivityRepository repository;

    public TenantActivityService(ITenantActivityRepository repository)
    {
        this.repository = repository;
    }

    public async Task AddAsync(ActivityRecord record, CancellationToken ct = default)
    {
        if (await repository.ExistsAsync(record.TenantId, record.SourceKey, ct))
            return;

        await repository.AddAsync(TenantActivityEntity.Create(record), ct);
    }

    public async Task<IReadOnlyList<ActivityItemDto>> GetRecentAsync(
        Guid tenantId,
        int take,
        CancellationToken ct = default) =>
        (await repository.GetRecentAsync(tenantId, take, ct))
            .Select(activity => new ActivityItemDto(
                activity.Id,
                activity.Type,
                activity.At,
                activity.Subject,
                activity.Detail,
                activity.Url))
            .ToArray();
}
