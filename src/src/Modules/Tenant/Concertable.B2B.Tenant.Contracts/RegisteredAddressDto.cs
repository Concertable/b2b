using System.Text.Json.Serialization;

namespace Concertable.B2B.Tenant.Contracts;

public sealed record RegisteredAddressDto
{
    public required string Line1 { get; init; }

    /// <summary>Optional second address line — omitted from the wire when absent (not serialized as null).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Line2 { get; init; }
    public required string City { get; init; }
    public required string Postcode { get; init; }
    public required string Country { get; init; }
}
