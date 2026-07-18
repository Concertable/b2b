using System.Text.Json.Serialization;

namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// A tenant's tax-compliance data — the shape for the org read, the update write, and the cross-module read
/// Concert uses to snapshot a supplier/customer onto a self-billed invoice. All-or-nothing: absent until
/// organization setup, then fully populated. <see cref="VatNumber"/> is the only optional field (absent = not
/// VAT-registered, a valid complete state).
/// </summary>
public sealed record TaxComplianceDto
{
    /// <summary>The seller's VAT number, or absent when not VAT-registered — many sellers aren't, so absence is a
    /// valid, complete state, not missing data. Omitted from the wire (not null) when absent; format validity
    /// (when present) is region-specific.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VatNumber { get; init; }
    public required string SellerIdentifier { get; init; }
    public required RegisteredAddressDto RegisteredAddress { get; init; }
    public required string BankReference { get; init; }
}
