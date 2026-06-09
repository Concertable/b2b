namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantService
{
    Task<TenantDto?> GetByIdAsync(Guid id);
}
