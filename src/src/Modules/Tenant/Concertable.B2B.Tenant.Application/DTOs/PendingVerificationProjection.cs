namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>A pending verification row joined with its tenant's type — an ephemeral query shape the admin
/// service enriches with the owning venue/artist's contact before returning <see cref="PendingVerificationDto"/>.</summary>
internal sealed record PendingVerificationProjection
{
    public required Guid TenantId { get; init; }
    public required TenantType TenantType { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
