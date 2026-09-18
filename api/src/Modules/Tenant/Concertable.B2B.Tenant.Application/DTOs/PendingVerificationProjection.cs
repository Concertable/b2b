namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>A pending verification row joined with its tenant's own legal and contact facts — an ephemeral
/// query shape the admin service returns as <see cref="PendingVerificationDto"/>. Joined rather than resolved
/// per row: the queue is a list, and a business with no marketplace profile still has both facts.</summary>
internal sealed record PendingVerificationProjection
{
    public required Guid TenantId { get; init; }
    public required string LegalName { get; init; }
    public required string ContactEmail { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
