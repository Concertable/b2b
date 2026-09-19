namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record PendingVerificationProjection
{
    public required Guid TenantId { get; init; }
    public required string LegalName { get; init; }
    public required string ContactEmail { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
