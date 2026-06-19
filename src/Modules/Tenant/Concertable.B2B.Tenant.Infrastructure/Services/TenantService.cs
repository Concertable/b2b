using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantService : ITenantService
{
    private readonly ITenantRepository repository;
    private readonly ITenantContext tenantContext;

    public TenantService(ITenantRepository repository, ITenantContext tenantContext)
    {
        this.repository = repository;
        this.tenantContext = tenantContext;
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

    public async Task<TenantDetails?> GetDetailsForCurrentTenantAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return null;

        var tenant = await repository.GetByIdAsync(tenantId, ct);
        return tenant?.ToDetails();
    }

    public async Task<TenantDetails> UpdateAsync(UpdateTenantRequest request, CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            throw new ForbiddenException("No tenant for current user.");

        var tenant = await repository.GetByIdAsync(tenantId, ct)
            ?? throw new NotFoundException($"Tenant {tenantId} not found.");

        tenant.UpdateLegalDetails(request.LegalName, request.Compliance.ToCompliance());
        await repository.SaveChangesAsync(ct);

        return tenant.ToDetails();
    }
}
