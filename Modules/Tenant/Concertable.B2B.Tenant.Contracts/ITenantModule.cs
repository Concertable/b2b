namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantModule
{
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
