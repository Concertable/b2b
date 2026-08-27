namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>A row of the admin verification-review queue, enriched with the owning venue/artist's contact —
/// null when the owning venue/artist could not be found (a data-integrity edge, not the ordinary case).</summary>
internal sealed record PendingVerificationDto
{
    public required Guid TenantId { get; init; }
    public required TenantType TenantType { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
