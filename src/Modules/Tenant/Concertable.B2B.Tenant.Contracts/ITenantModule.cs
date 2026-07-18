namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantModule
{
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The caller's memberships — feeds the <c>/api/auth/me</c> tenant switcher payload.</summary>
    Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Whether the tenant holds a complete, jurisdiction-valid seller tax identity — the single source of
    /// truth (resolved per jurisdiction inside the Tenant module) that the fail-closed payout gate and the dashboard
    /// nag both consume. Fail-closed: a missing tenant or absent/invalid compliance is not complete.</summary>
    Task<bool> IsTaxComplianceCompleteAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>The tenant's tax-compliance details — the cross-module read Concert uses to snapshot a supplier or
    /// customer onto a self-billed invoice. Null when the tenant is unknown or its tax compliance is not yet captured.</summary>
    Task<TaxComplianceDto?> GetTaxComplianceAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>The VAT decomposition of a VAT-inclusive <paramref name="gross"/> for the supplier tenant — reads the
    /// tenant's VAT-registration status internally and applies the region VAT policy (registered ⇒ decompose;
    /// unregistered ⇒ <see cref="VatCalculation.None"/>). Throws if the tenant or its compliance is absent, since the
    /// settlement tax-gate guarantees both are present by invoice time.</summary>
    Task<VatCalculation> GetVatCalculationAsync(Guid tenantId, decimal gross, CancellationToken ct = default);
}
