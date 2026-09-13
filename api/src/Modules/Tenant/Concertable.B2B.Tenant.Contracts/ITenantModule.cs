namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantModule
{
    Task<Option<TenantDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetMemberUserIdsAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> IsTaxComplianceCompleteAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> IsVerifiedAsync(Guid tenantId, CancellationToken ct = default);
    Task<Option<TaxComplianceDto>> GetTaxComplianceAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<VatCalculation, VatCalculationError>> GetVatCalculationAsync(
        Guid tenantId,
        decimal gross,
        CancellationToken ct = default);
    Task<IReadOnlyList<ActivityItemDto>> GetRecentActivityAsync(
        Guid tenantId,
        int take,
        CancellationToken ct = default);
}
