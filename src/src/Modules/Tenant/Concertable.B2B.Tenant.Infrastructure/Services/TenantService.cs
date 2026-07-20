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
    private readonly IVatPolicy vatPolicy;

    public TenantService(ITenantRepository repository, ITenantContext tenantContext, IVatPolicy vatPolicy)
    {
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.vatPolicy = vatPolicy;
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

        // VAT-number format is enforced by UpdateTenantRequestValidator in the write pipeline, so the request is valid here.
        tenant.UpdateLegalDetails(request.LegalName, request.TaxCompliance.ToTaxCompliance());
        await repository.SaveChangesAsync(ct);

        return ToDetails(tenant);
    }

    public async Task DeleteCurrentTenantAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var tenant = await repository.GetByIdAsync(tenantId, ct)
            ?? throw new NotFoundException($"Tenant {tenantId} not found.");

        foreach (var membership in await repository.ListMembershipsByTenantAsync(tenantId, ct))
            repository.RemoveMembership(membership);

        foreach (var invitation in await repository.ListInvitationsByTenantAsync(tenantId, ct))
            repository.RemoveInvitation(invitation);

        repository.Remove(tenant);
        await repository.SaveChangesAsync(ct);
    }

    public async Task<bool> IsTaxComplianceCompleteAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Presence IS completeness — the write path enforces the required fields + VAT format, so stored data
        // is always complete. Single source of truth, shared with the org read's nag.
        var tenant = await repository.GetByIdAsync(tenantId, ct);
        return tenant?.TaxCompliance is not null;
    }

    public async Task<TaxComplianceDto?> GetTaxComplianceAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(tenantId, ct);
        return tenant?.TaxCompliance?.ToDto();
    }

    public async Task<VatCalculation> GetVatCalculationAsync(Guid tenantId, decimal gross, CancellationToken ct = default)
    {
        // Fail-closed: settlement's tax-gate guarantees tenant + compliance by invoice time; a null VatNumber (unregistered) is the only valid absence.
        var tenant = await repository.GetByIdAsync(tenantId, ct)
            ?? throw new NotFoundException($"Tenant {tenantId} not found.");
        var compliance = tenant.TaxCompliance
            ?? throw new InvalidOperationException(
                $"Tenant {tenantId} has no tax compliance; the settlement tax-gate should guarantee it by invoice time.");

        return vatPolicy.Apply(gross, compliance.VatNumber);
    }

    private TenantDetails ToDetails(TenantEntity tenant) => new()
    {
        Id = tenant.Id,
        LegalName = tenant.LegalName,
        TaxCompliance = tenant.TaxCompliance?.ToDto(),
    };
}
