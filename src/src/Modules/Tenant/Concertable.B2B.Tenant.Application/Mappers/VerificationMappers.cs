using Concertable.B2B.Tenant.Application.DTOs;

namespace Concertable.B2B.Tenant.Application.Mappers;

internal static class VerificationMappers
{
    public static VerificationStatusDto ToDto(this TenantVerificationEntity verification) => new()
    {
        Status = verification.Status,
        RejectionReason = verification.RejectionReason,
        SubmittedAt = verification.SubmittedAt,
        Documents = verification.Documents.Select(d => d.ToDto()).ToList(),
    };

    public static VerificationDocumentDto ToDto(this VerificationDocumentEntity document) => new()
    {
        DocumentType = document.DocumentType,
        UploadedAt = document.UploadedAt,
    };
}
