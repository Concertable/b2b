using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Dac7;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantService : ITenantService
{
    private readonly ITenantRepository repository;
    private readonly ITenantContext tenantContext;
    private readonly IDac7Strategy dac7;

    public TenantService(ITenantRepository repository, ITenantContext tenantContext, IDac7Strategy dac7)
    {
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.dac7 = dac7;
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(id, ct);
        return tenant?.ToDto();
    }

    public async Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default)
    {
        var memberships = await repository.GetMembershipsAsync(userId, ct);
        return memberships
            .Select(m => new MembershipDto(m.TenantId, m.LegalName, m.Type, m.Role))
            .ToList();
    }

    public async Task<TenantDetails?> GetDetailsForCurrentTenantAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return null;

        var tenant = await repository.GetByIdAsync(tenantId, ct);
        return tenant is null ? null : tenant.ToDetails(BuildDac7(tenant));
    }

    public async Task<TenantDetails> UpdateAsync(UpdateTenantRequest request, CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            throw new ForbiddenException("No tenant for current user.");

        var tenant = await repository.GetByIdAsync(tenantId, ct)
            ?? throw new NotFoundException($"Tenant {tenantId} not found.");

        // Jurisdiction-specific format check + message, both sourced from the tenant's own Jurisdiction (the
        // org-form validator can't see it). A null VAT number just means not VAT-registered — nothing to check.
        if (!string.IsNullOrWhiteSpace(request.Compliance.VatNumber)
            && !dac7.IsValidVatNumber(tenant.Jurisdiction, request.Compliance.VatNumber))
            throw new BadRequestException(dac7.DescribeVatNumberRequirement(tenant.Jurisdiction));

        tenant.UpdateLegalDetails(request.LegalName, request.Compliance.ToCompliance());
        await repository.SaveChangesAsync(ct);

        return tenant.ToDetails(BuildDac7(tenant));
    }

    public async Task<bool> IsDac7CompleteAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(tenantId, ct);
        return tenant is not null && dac7.IsComplete(tenant.Jurisdiction, tenant.Compliance);
    }

    private Dac7Dto BuildDac7(TenantEntity tenant)
    {
        var labels = dac7.GetFieldLabels(tenant.Jurisdiction);
        return new Dac7Dto
        {
            Complete = dac7.IsComplete(tenant.Jurisdiction, tenant.Compliance),
            SellerIdentifierLabel = labels.SellerIdentifierLabel,
            SellerIdentifierHint = labels.SellerIdentifierHint,
            VatLabel = labels.VatLabel,
            VatNumberPlaceholder = labels.VatNumberPlaceholder,
        };
    }
}
