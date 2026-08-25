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

    [Fact]
    public void Create_BlobNameTooLong_ThrowsDomainException()
    {
        var blobName = new string('b', 501);

        Assert.Throws<DomainException>(() =>
            VerificationDocumentEntity.Create(VerificationDocumentType.Licence, blobName, UploadedAt));
    }

    [Fact]
    public void BuildBlobName_IncludesTenantDocumentTypeAndExtension()
    {
        var tenantId = Guid.NewGuid();

        var blobName = VerificationDocumentEntity.BuildBlobName(tenantId, VerificationDocumentType.Licence, ".pdf");

        Assert.StartsWith($"verification-evidence/{tenantId}-Licence-", blobName);
        Assert.EndsWith(".pdf", blobName);
    }

    [Fact]
    public void BuildBlobName_CalledTwice_ReturnsDistinctNames()
    {
        var tenantId = Guid.NewGuid();

        var first = VerificationDocumentEntity.BuildBlobName(tenantId, VerificationDocumentType.Licence, ".pdf");
        var second = VerificationDocumentEntity.BuildBlobName(tenantId, VerificationDocumentType.Licence, ".pdf");

        Assert.NotEqual(first, second);
    }
}
