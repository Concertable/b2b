using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantService
{
    Task<Option<TenantDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetMemberUserIdsAsync(Guid tenantId, CancellationToken ct = default);

    Task<Option<TenantDetails>> GetDetailsAsync(CancellationToken ct = default);

    Task<Result<TenantDetails, UpdateTenantError>> UpdateAsync(UpdateTenantRequest request, CancellationToken ct = default);

    Task<UnitResult<DeleteTenantError>> DeleteAsync(CancellationToken ct = default);

    Task<bool> IsTaxComplianceCompleteAsync(Guid tenantId, CancellationToken ct = default);

    Task<Option<TaxComplianceDto>> GetTaxComplianceAsync(Guid tenantId, CancellationToken ct = default);

    Task<Result<VatCalculation, VatCalculationError>> GetVatCalculationAsync(
        Guid tenantId,
        decimal gross,
        CancellationToken ct = default);
}
