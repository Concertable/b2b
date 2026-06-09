namespace Concertable.B2B.Tenant.Infrastructure;

internal sealed class TenantModule : ITenantModule
{
    private readonly ITenantService service;

    public TenantModule(ITenantService service)
    {
        this.service = service;
    }

    public Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        service.GetByIdAsync(id, ct);

    public Task<Guid?> GetTenantIdByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        service.GetTenantIdByUserIdAsync(userId, ct);
}
