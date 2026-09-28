namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantDeletionGuard
{
    Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default);
}
