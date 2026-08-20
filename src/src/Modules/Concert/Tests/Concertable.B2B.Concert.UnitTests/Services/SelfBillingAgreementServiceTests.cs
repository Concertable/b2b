using System.Net;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Tenant.Contracts;
using Reunion;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class SelfBillingAgreementServiceTests
{
    private readonly Mock<ISelfBillingAgreementRepository> repository = new();
    private readonly Mock<ITenantModule> tenantModule = new();
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly Mock<IClientContext> clientContext = new();
    private readonly Mock<IPdfBlobCache> pdfCache = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly Guid supplierTenantId = Guid.NewGuid();
    private readonly Guid userId = Guid.NewGuid();
    private readonly SelfBillingAgreementService service;

    public SelfBillingAgreementServiceTests()
    {
        tenantContext.SetupGet(t => t.TenantId).Returns(supplierTenantId);
        currentUser.SetupGet(u => u.Id).Returns(userId);
        clientContext.SetupGet(c => c.IpAddress).Returns(IPAddress.Loopback);
        clientContext.SetupGet(c => c.UserAgent).Returns("supplier-agent");
        tenantModule.Setup(m => m.GetByIdAsync(supplierTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.Some(new TenantDto(supplierTenantId, "Sally Supplier Ltd")));
        tenantModule.Setup(m => m.GetTaxComplianceAsync(supplierTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.Some(new TaxComplianceDto
            {
                VatNumber = "GB123456789",
                SellerIdentifier = "SELL-1",
                BankReference = "BR-1",
                HoldsMusicLicence = true,
                RegisteredAddress = new RegisteredAddressDto
                {
                    Line1 = "1 Road",
                    City = "Town",
                    Postcode = "AB1 2CD",
                    Country = "United Kingdom",
                },
            }));

        service = new SelfBillingAgreementService(
            repository.Object,
            tenantModule.Object,
            currentUser.Object,
            clientContext.Object,
            pdfCache.Object,
            tenantContext.Object,
            Options.Create(new LegalSettings { PlatformTermsVersion = "2026-07" }),
            timeProvider,
            NullLogger<SelfBillingAgreementService>.Instance);
    }

    [Fact]
    public async Task GrantAsync_FreezesSupplierIdentityAndTwelveMonthWindow()
    {
        SelfBillingAgreementEntity? built = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<SelfBillingAgreementEntity>(), It.IsAny<CancellationToken>()))
            .Callback<SelfBillingAgreementEntity, CancellationToken>((a, _) => built = a)
            .ReturnsAsync((SelfBillingAgreementEntity a, CancellationToken _) => a);

        var result = await service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" });

        Assert.True(result.IsSuccess);
        Assert.NotNull(built);
        Assert.Equal(supplierTenantId, built.TenantId);
        Assert.Equal("Sally Supplier Ltd", built.Supplier.LegalName);
        Assert.Equal("GB123456789", built.Supplier.VatNumber);
        Assert.Equal("1 Road", built.Supplier.AddressLine1);
        Assert.Equal(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc), built.AcceptedAtUtc);
        Assert.Equal(new DateTime(2027, 2, 1, 12, 0, 0, DateTimeKind.Utc), built.ExpiresAtUtc);
        Assert.Equal("2026-07", built.PlatformTermsVersion);
        Assert.Contains("Sally Supplier Ltd", built.ClauseText);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GrantAsync_RecordsSupplierESignatureFromRequestAndServerContext()
    {
        SelfBillingAgreementEntity? built = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<SelfBillingAgreementEntity>(), It.IsAny<CancellationToken>()))
            .Callback<SelfBillingAgreementEntity, CancellationToken>((a, _) => built = a)
            .ReturnsAsync((SelfBillingAgreementEntity a, CancellationToken _) => a);

        var result = await service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" });

        Assert.True(result.IsSuccess);
        Assert.NotNull(built);
        Assert.Equal("Sally Supplier", built.SupplierESignature.SignatoryName);
        Assert.Equal(userId, built.SupplierESignature.UserId);
        Assert.Equal(IPAddress.Loopback, built.SupplierESignature.Ip);
    }

    [Fact]
    public async Task GrantAsync_WithoutTaxCompliance_ReturnsTypedError()
    {
        tenantModule.Setup(m => m.GetTaxComplianceAsync(supplierTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.None<TaxComplianceDto>());

        var result = await service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<GrantSelfBillingAgreementError.MissingTaxCompliance>(error);
    }

    [Fact]
    public async Task GrantAsync_WithoutTenant_ReturnsTypedError()
    {
        tenantContext.SetupGet(t => t.TenantId).Returns((Guid?)null);

        var result = await service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<GrantSelfBillingAgreementError.MissingTenant>(error);
    }

    [Fact]
    public async Task GrantAsync_WithoutTenantRecord_ReturnsTypedError()
    {
        tenantModule.Setup(m => m.GetByIdAsync(supplierTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.None<TenantDto>());

        var result = await service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" });

        Assert.True(result.TryGetError(out var error));
        var missing = Assert.IsType<GrantSelfBillingAgreementError.TenantNotFound>(error);
        Assert.Equal(supplierTenantId, missing.TenantId);
    }

    [Fact]
    public async Task GrantAsync_WithoutUser_ReturnsTypedError()
    {
        currentUser.SetupGet(u => u.Id).Returns((Guid?)null);

        var result = await service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<GrantSelfBillingAgreementError.MissingUser>(error);
    }

    [Fact]
    public async Task GetStatusAsync_LatestAgreement_ReturnsCurrentStatus()
    {
        var emptyStatus = await service.GetStatusAsync();

        Assert.Null(emptyStatus.Agreement);
        Assert.False(emptyStatus.IsInForce);
        Assert.False(emptyStatus.CanRenew);

        repository.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAgreement());

        var status = await service.GetStatusAsync();

        Assert.NotNull(status.Agreement);
        Assert.Equal("Sally Supplier Ltd", status.Agreement.SupplierLegalName);
        Assert.Equal(
            new DateTime(2027, 2, 1, 12, 0, 0, DateTimeKind.Utc),
            status.Agreement.ExpiresAtUtc);
        Assert.True(status.IsInForce);
        Assert.False(status.CanRenew);
    }

    [Fact]
    public async Task GetPdfAsync_RendersCurrentAgreementLazily_OrReturnsNotFoundWhenNone()
    {
        var missing = await service.GetPdfAsync();

        Assert.True(missing.TryGetError(out var error));
        Assert.IsType<SelfBillingAgreementPdfError.NotFound>(error);

        var agreement = BuildAgreement();
        repository.Setup(r => r.GetCurrentAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agreement);
        var bytes = new byte[] { 1, 2, 3 };
        var blobName = Assert.IsType<string>(agreement.PdfBlobName);
        pdfCache.Setup(c => c.GetOrCreateAsync(blobName, It.IsAny<QuestPDF.Infrastructure.IDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        var result = await service.GetPdfAsync();

        Assert.True(result.TryGetValue(out var download));
        Assert.Equal(bytes, download.Content);
        Assert.Contains("self-billing-agreement", download.FileName);
    }

    private SelfBillingAgreementEntity BuildAgreement() =>
        SelfBillingAgreementEntity.Create(
            supplierTenantId,
            new InvoiceParty(supplierTenantId, "Sally Supplier Ltd", "GB123456789", "1 Road", null, "Town", "AB1 2CD", "United Kingdom"),
            new ESignature(userId, timeProvider.GetUtcNow().UtcDateTime, IPAddress.Loopback, "agent", "Sally Supplier", null),
            "clause",
            "2026-07",
            timeProvider.GetUtcNow().UtcDateTime,
            timeProvider.GetUtcNow().UtcDateTime);
}
