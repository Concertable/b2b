using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Concertable.Shared.Blob.Application;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class VerificationServiceTests
{
    private readonly Mock<IVerificationRepository> repository;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly Mock<IBlobStorageService> blobStorage;
    private readonly VerificationService service;

    public VerificationServiceTests()
    {
        this.repository = new Mock<IVerificationRepository>();
        this.tenantContext = new Mock<ITenantContext>();
        this.blobStorage = new Mock<IBlobStorageService>();
        this.service = new VerificationService(
            repository.Object,
            tenantContext.Object,
            blobStorage.Object,
            TimeProvider.System);
    }

    private static IReadOnlyList<EvidenceUpload> BuildUploads() =>
        [new EvidenceUpload(Stream.Null, ".pdf", VerificationDocumentType.Licence)];

    #region GetStatusAsync

    [Fact]
    public async Task GetStatusAsync_NoActiveTenant_ReturnsNone()
    {
        var result = await service.GetStatusAsync();

        Assert.True(result.IsNone);
        repository.Verify(
            r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetStatusAsync_NeverSubmitted_ReturnsNone()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(c => c.TenantId).Returns(tenantId);
        repository
            .Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantVerificationEntity?)null);

        var result = await service.GetStatusAsync();

        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task GetStatusAsync_ExistingRow_MapsStatusAndDocuments()
    {
        var tenantId = Guid.NewGuid();
        var verification = TenantVerificationEntity.Submit(
            tenantId,
            [VerificationDocumentEntity.Create(VerificationDocumentType.Licence, "blob-1", DateTime.UtcNow)],
            DateTime.UtcNow);
        tenantContext.SetupGet(c => c.TenantId).Returns(tenantId);
        repository
            .Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        var result = await service.GetStatusAsync();

        Assert.True(result.TryGetValue(out var dto));
        Assert.Equal(TenantVerificationStatus.Pending, dto.Status);
        Assert.Single(dto.Documents);
        Assert.Equal(VerificationDocumentType.Licence, dto.Documents[0].DocumentType);
    }

    #endregion

    #region SubmitAsync

    [Fact]
    public async Task SubmitAsync_NoExistingRow_UploadsEvidenceAndCreatesPendingVerification()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(c => c.TenantId).Returns(tenantId);
        repository
            .Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantVerificationEntity?)null);
        repository
            .Setup(r => r.InsertAsync(It.IsAny<TenantVerificationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantVerificationEntity entity, CancellationToken _) => entity);

        var result = await service.SubmitAsync(BuildUploads());

        Assert.True(result.TryGetValue(out var dto));
        Assert.Equal(TenantVerificationStatus.Pending, dto.Status);
        blobStorage.Verify(
            b => b.UploadAsync(It.IsAny<Stream>(), It.Is<string>(name => name.Contains(tenantId.ToString()))),
            Times.Once);
        repository.Verify(
            r => r.InsertAsync(It.IsAny<TenantVerificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(TenantVerificationStatus.Pending)]
    [InlineData(TenantVerificationStatus.Approved)]
    public async Task SubmitAsync_NotRejected_ReturnsNotEligibleWithoutUploading(TenantVerificationStatus status)
    {
        var tenantId = Guid.NewGuid();
        var verification = TenantVerificationEntity.Submit(
            tenantId,
            [VerificationDocumentEntity.Create(VerificationDocumentType.Licence, "blob-1", DateTime.UtcNow)],
            DateTime.UtcNow);
        if (status == TenantVerificationStatus.Approved)
            verification.Approve(Guid.NewGuid(), DateTime.UtcNow);
        tenantContext.SetupGet(c => c.TenantId).Returns(tenantId);
        repository
            .Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        var result = await service.SubmitAsync(BuildUploads());

        Assert.True(result.TryGetError(out var error));
        var notEligible = Assert.IsType<SubmitVerificationError.NotEligible>(error);
        Assert.Equal(status, notEligible.Status);
        blobStorage.Verify(
            b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_Rejected_ResubmitsAndReturnsPending()
    {
        var tenantId = Guid.NewGuid();
        var verification = TenantVerificationEntity.Submit(
            tenantId,
            [VerificationDocumentEntity.Create(VerificationDocumentType.Licence, "blob-1", DateTime.UtcNow)],
            DateTime.UtcNow);
        verification.Reject(Guid.NewGuid(), "Illegible scan.", DateTime.UtcNow);
        tenantContext.SetupGet(c => c.TenantId).Returns(tenantId);
        repository
            .Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        var result = await service.SubmitAsync(BuildUploads());

        Assert.True(result.TryGetValue(out var dto));
        Assert.Equal(TenantVerificationStatus.Pending, dto.Status);
        Assert.Null(dto.RejectionReason);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(
            r => r.InsertAsync(It.IsAny<TenantVerificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
