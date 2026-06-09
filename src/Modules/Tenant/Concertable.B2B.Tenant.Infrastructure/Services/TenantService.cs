namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantService : ITenantService
{
    private readonly ITenantRepository repository;

    public TenantService(ITenantRepository repository)
    {
        this.repository = repository;
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id)
    {
        var tenant = await repository.GetByIdAsync(id);
        return tenant?.ToDto();
    }
}
