namespace Concertable.B2B.Tenant.Contracts;

public sealed class TenantUnavailableException(Guid tenantId)
    : Exception($"Tenant {tenantId} is no longer available.")
{
    public Guid TenantId { get; } = tenantId;
}
