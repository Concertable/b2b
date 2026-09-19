using System.Text.Json.Serialization;

namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record TenantDetails
{
    public required Guid Id { get; init; }
    public required string LegalName { get; init; }
    public required string ContactEmail { get; init; }
    public required long Version { get; init; }
    public required long EligibilityVersion { get; init; }

    public required IReadOnlyList<TenantBusinessActivityKind> BusinessActivities { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TaxComplianceDto? TaxCompliance { get; init; }
}
