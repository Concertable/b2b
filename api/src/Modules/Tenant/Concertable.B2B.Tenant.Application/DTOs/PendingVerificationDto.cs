namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>A row of the admin verification-review queue, carrying the tenant's own legal name and contact.</summary>
internal sealed record PendingVerificationDto
{
    public required Guid TenantId { get; init; }
    public required string LegalName { get; init; }
    public required string ContactEmail { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
