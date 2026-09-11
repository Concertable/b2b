namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantActivityService
{
    Task AddAsync(ActivityRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityItemDto>> GetRecentAsync(Guid tenantId, int take, CancellationToken ct = default);
}
