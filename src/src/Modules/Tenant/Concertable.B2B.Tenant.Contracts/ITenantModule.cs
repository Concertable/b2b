namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantModule
{
    Task<Option<TenantDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The caller's memberships — feeds the <c>/api/auth/me</c> tenant switcher payload.</summary>
    Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Every member user id of a tenant — the inverse of <see cref="GetMembershipsAsync"/>. Drives the
    /// group-inbox fan-out (one SignalR ping + one email copy per member of a message's recipient tenant).</summary>
    Task<IReadOnlyList<Guid>> GetMemberUserIdsAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Whether the tenant holds a complete, jurisdiction-valid seller tax identity — the single source of
    /// truth (resolved per jurisdiction inside the Tenant module) that the fail-closed payout gate and the dashboard
    /// nag both consume. Fail-closed: a missing tenant or absent/invalid compliance is not complete.</summary>
    Task<bool> IsTaxComplianceCompleteAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>The tenant's tax-compliance details — the cross-module read Concert uses to snapshot a supplier or
    /// customer onto a self-billed invoice. Returns <c>Option.None</c> when the tenant is unknown or its tax compliance
    /// is not yet captured.</summary>
    Task<Option<TaxComplianceDto>> GetTaxComplianceAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>The VAT decomposition of a VAT-inclusive <paramref name="gross"/> for the supplier tenant — reads the
    /// tenant's VAT-registration status internally and applies the region VAT policy (registered ⇒ decompose;
    /// unregistered ⇒ <see cref="VatCalculation.None"/>). Returns a failed result containing
    /// <see cref="VatCalculationError.TenantNotFound"/> when the tenant is unknown. Throws
    /// <see cref="InvalidOperationException"/> when compliance is absent because the settlement tax-gate guarantees it
    /// is present by invoice time.</summary>
    Task<Result<VatCalculation, VatCalculationError>> GetVatCalculationAsync(
        Guid tenantId,
        decimal gross,
        CancellationToken ct = default);
}
