namespace Concertable.B2B.Tenant.Application.DTOs;

public sealed record TenantDetails
{
    public required Guid Id { get; init; }
    public required string LegalName { get; init; }

    /// <summary>The stored tax-compliance data (form pre-fill); null until organization setup is completed.</summary>
    public TaxComplianceDto? TaxCompliance { get; init; }

    /// <summary>The derived nag flag — the same completeness rule the fail-closed payout gate consumes.</summary>
    public required bool TaxComplete { get; init; }

    /// <summary>The region's field labels the org form renders (region config, not per-tenant data).</summary>
    public required TaxFormLabels FormLabels { get; init; }
}
