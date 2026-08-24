using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class VerificationDocumentEntityTests
{
    private static readonly DateTime UploadedAt = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithBlobName_PreservesTheGivenValues()
    {
        var document = VerificationDocumentEntity.Create(
            VerificationDocumentType.ProofOfAddress, "verification-evidence/abc.pdf", UploadedAt);

        Assert.Equal(VerificationDocumentType.ProofOfAddress, document.DocumentType);
        Assert.Equal("verification-evidence/abc.pdf", document.BlobName);
        Assert.Equal(UploadedAt, document.UploadedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NoBlobName_ThrowsDomainException(string? blobName)
    {
        Assert.Throws<DomainException>(() =>
            VerificationDocumentEntity.Create(VerificationDocumentType.Licence, blobName!, UploadedAt));
    }
}
