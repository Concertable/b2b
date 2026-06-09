namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantService : ITenantService
{
    private readonly ITenantRepository repository;

    public TenantService(ITenantRepository repository)
    {
        this.repository = repository;
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(id, ct);
        return tenant?.ToDto();
    }

    public async Task<Guid?> GetTenantIdByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var tenant = await repository.GetByCreatedByUserIdAsync(userId, ct);
        return tenant?.Id;
    }
}
