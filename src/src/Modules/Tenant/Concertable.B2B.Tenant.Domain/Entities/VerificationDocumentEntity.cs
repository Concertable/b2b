using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

/// <summary>
/// One piece of evidence submitted against a <see cref="TenantVerificationEntity"/>. Append-only: a
/// resubmission adds new rows rather than replacing prior ones, so the admin trail shows what was
/// reviewed each time. <see cref="TenantVerificationId"/> is set by EF from the owning aggregate's
/// collection, never by domain code.
/// </summary>
public sealed class VerificationDocumentEntity : IIdEntity
{
    private VerificationDocumentEntity() { }

    public int Id { get; private set; }
    public Guid TenantVerificationId { get; private set; }
    public VerificationDocumentType DocumentType { get; private set; }
    public string BlobName { get; private set; } = null!;
    public DateTime UploadedAt { get; private set; }

    public static VerificationDocumentEntity Create(VerificationDocumentType documentType, string blobName, DateTime uploadedAt)
    {
        DomainException.ThrowIfNullOrWhiteSpace(blobName, "BlobName");
        if (blobName.Length > 500)
            throw new DomainException("BlobName must be 500 characters or fewer.");

        return new VerificationDocumentEntity
        {
            DocumentType = documentType,
            BlobName = blobName,
            UploadedAt = uploadedAt,
        };
    }

    /// <summary>Creates evidence for an uploaded file, deriving its own blob name — the naming convention
    /// is the domain's rule, not infrastructure's to invent inline. <paramref name="fileExtension"/>
    /// includes the leading dot (as <see cref="Path.GetExtension"/> returns it), or is empty.</summary>
    public static VerificationDocumentEntity Create(
        Guid tenantId, VerificationDocumentType documentType, string fileExtension, DateTime uploadedAt) =>
        Create(documentType, $"verification-evidence/{tenantId}-{documentType}-{Guid.NewGuid()}{fileExtension}", uploadedAt);
}
