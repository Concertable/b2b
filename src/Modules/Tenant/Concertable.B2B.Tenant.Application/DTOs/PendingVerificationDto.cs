using System.Text.Json.Serialization;

namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>A row of the admin verification-review queue, enriched with the owning venue/artist's contact —
/// absent when the owning venue/artist could not be found (a data-integrity edge, not the ordinary case).
/// Name and email are both-or-neither, so they travel as one optional group rather than two nullable fields;
/// omitted from the wire when absent, so the client sees no field rather than a null one.</summary>
internal sealed record PendingVerificationDto
{
    public required Guid TenantId { get; init; }
    public required TenantType TenantType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TenantContact? Contact { get; init; }

    public required DateTime SubmittedAt { get; init; }
}
