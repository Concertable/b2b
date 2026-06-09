namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantService
{
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Guid?> GetTenantIdByUserIdAsync(Guid userId, CancellationToken ct = default);
}
