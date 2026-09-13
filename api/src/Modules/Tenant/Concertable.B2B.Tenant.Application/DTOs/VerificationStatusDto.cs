using Concertable.B2B.Tenant.Domain.Enums;

namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record VerificationStatusDto
{
    public required TenantVerificationStatus Status { get; init; }
    public string? RejectionReason { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required IReadOnlyList<VerificationDocumentDto> Documents { get; init; }
}

internal sealed record VerificationDocumentDto
{
    public required VerificationDocumentType DocumentType { get; init; }
    public required DateTime UploadedAt { get; init; }
}
