using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Tax;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantService : ITenantService
{
    private readonly ITenantRepository repository;
    private readonly ITenantContext tenantContext;
    private readonly ITaxComplianceRules taxRules;

    public TenantService(ITenantRepository repository, ITenantContext tenantContext, ITaxComplianceRules taxRules)
    {
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.taxRules = taxRules;
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
        return tenant is null ? null : ToDetails(tenant);
    }

    public async Task<TenantDetails> UpdateAsync(UpdateTenantRequest request, CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            throw new ForbiddenException("No tenant for current user.");

        var tenant = await repository.GetByIdAsync(tenantId, ct)
            ?? throw new NotFoundException($"Tenant {tenantId} not found.");

        // Region-specific VAT format check + message, from the deployment's tax-compliance rules (the org-form
        // validator is region-agnostic). A null VAT number just means not VAT-registered — nothing to check.
        if (!string.IsNullOrWhiteSpace(request.TaxCompliance.VatNumber)
            && !taxRules.IsValidVatNumber(request.TaxCompliance.VatNumber))
            throw new BadRequestException(taxRules.DescribeVatNumberRequirement());

        tenant.UpdateLegalDetails(request.LegalName, request.TaxCompliance.ToTaxCompliance());
        await repository.SaveChangesAsync(ct);

        return ToDetails(tenant);
    }

    public async Task<bool> IsTaxComplianceCompleteAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(tenantId, ct);
        return tenant is not null && taxRules.IsComplete(tenant.TaxCompliance);
    }

    private TenantDetails ToDetails(TenantEntity tenant) =>
        tenant.ToDetails(taxRules.IsComplete(tenant.TaxCompliance), taxRules.GetFieldLabels());
}
