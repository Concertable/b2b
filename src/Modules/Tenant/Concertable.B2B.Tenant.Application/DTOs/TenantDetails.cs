using System.Text.Json.Serialization;

namespace Concertable.B2B.Tenant.Application.DTOs;

public sealed record TenantDetails
{
    public required Guid Id { get; init; }
    public required string LegalName { get; init; }

    /// <summary>The tenant's tax data — absent until organization setup. Its presence IS completeness: the write
    /// path enforces the required fields + VAT-number format, so anything stored is already complete. Omitted from
    /// the wire when absent (not serialized as null), so the client sees an optional field, not a null.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TaxComplianceDto? TaxCompliance { get; init; }
}
