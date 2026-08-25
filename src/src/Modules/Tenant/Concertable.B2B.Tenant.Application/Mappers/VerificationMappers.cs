using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;

namespace Concertable.B2B.Tenant.Application.Mappers;

internal static class VerificationMappers
{
    extension(TenantVerificationEntity verification)
    {
        public VerificationStatusDto ToDto() => new()
        {
            Status = verification.Status,
            RejectionReason = verification.RejectionReason,
            SubmittedAt = verification.SubmittedAt,
            Documents = verification.Documents.Select(d => d.ToDto()).ToList(),
        };
    }

    extension(VerificationDocumentEntity document)
    {
        public VerificationDocumentDto ToDto() => new()
        {
            DocumentType = document.DocumentType,
            UploadedAt = document.UploadedAt,
        };
    }

    extension(SubmitVerificationRequest request)
    {
        public IReadOnlyList<EvidenceUpload> ToEvidenceUploads() =>
            request.Files
                .Select((file, i) => new EvidenceUpload(
                    file.OpenReadStream(),
                    Path.GetExtension(file.FileName),
                    request.DocumentTypes[i]))
                .ToList();
    }
}
