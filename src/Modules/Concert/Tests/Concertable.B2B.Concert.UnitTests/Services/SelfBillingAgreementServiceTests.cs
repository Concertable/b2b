using System.Net;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
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
            .ReturnsAsync(new TenantDto(supplierTenantId, "Sally Supplier Ltd"));
        tenantModule.Setup(m => m.GetTaxComplianceAsync(supplierTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaxComplianceDto
            {
                VatNumber = "GB123456789",
                SellerIdentifier = "SELL-1",
                BankReference = "BR-1",
                RegisteredAddress = new RegisteredAddressDto
                {
                    Line1 = "1 Road", City = "Town", Postcode = "AB1 2CD", Country = "United Kingdom",
                },
            });

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

        await service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" });

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

        await service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" });

        Assert.Equal("Sally Supplier", built!.SupplierESignature.SignatoryName);
        Assert.Equal(userId, built.SupplierESignature.UserId);
        Assert.Equal(IPAddress.Loopback, built.SupplierESignature.Ip);
    }

    [Fact]
    public async Task GrantAsync_WithoutTaxCompliance_ThrowsBadRequest()
    {
        tenantModule.Setup(m => m.GetTaxComplianceAsync(supplierTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxComplianceDto?)null);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" }));
    }

    [Fact]
    public async Task GrantAsync_WithoutTenant_ThrowsForbidden()
    {
        tenantContext.SetupGet(t => t.TenantId).Returns((Guid?)null);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GrantAsync(new ESignatureRequest { SignatoryName = "Sally Supplier" }));
    }

    [Fact]
    public async Task GetLatestAsync_MapsLatestAgreement_OrNullWhenNone()
    {
        Assert.Null(await service.GetLatestAsync());

        repository.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAgreement());

        var dto = await service.GetLatestAsync();

        Assert.NotNull(dto);
        Assert.Equal("Sally Supplier Ltd", dto.SupplierLegalName);
        Assert.Equal(new DateTime(2027, 2, 1, 12, 0, 0, DateTimeKind.Utc), dto.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetPdfAsync_RendersCurrentAgreementLazily_OrThrowsNotFoundWhenNone()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPdfAsync());

        var agreement = BuildAgreement();
        repository.Setup(r => r.GetCurrentAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agreement);
        var bytes = new byte[] { 1, 2, 3 };
        pdfCache.Setup(c => c.GetOrCreateAsync(agreement.PdfBlobName!, It.IsAny<QuestPDF.Infrastructure.IDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        var download = await service.GetPdfAsync();

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
