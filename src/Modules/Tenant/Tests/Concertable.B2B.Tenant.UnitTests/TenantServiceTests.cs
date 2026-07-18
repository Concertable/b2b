using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Tax;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantServiceTests
{
    private readonly Mock<ITenantRepository> repository;
    private readonly TenantService service;

    public TenantServiceTests()
    {
        this.repository = new Mock<ITenantRepository>();
        this.service = new TenantService(repository.Object, Mock.Of<ITenantContext>(), new VatPolicy(new UkVatCalculator()));
    }

    private static TenantEntity Bare() =>
        TenantEntity.Create("bare@test.com", Guid.NewGuid(), TenantType.Venue, DateTime.UtcNow);

    private static TenantEntity Onboarded(string? vatNumber)
    {
        var tenant = Bare();
        tenant.UpdateLegalDetails("Acme Ltd", new TaxCompliance(
            vatNumber,
            "SID000001",
            new RegisteredAddress("1 Main St", "Floor 2", "London", "EC1A 1AA", "United Kingdom"),
            "GB00BANK00000000000001"));
        return tenant;
    }

    #region GetVatCalculationAsync

    [Fact]
    public async Task GetVatCalculationAsync_RegisteredSupplier_DecomposesInclusiveGross()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Onboarded("GB123456789"));

        var result = await service.GetVatCalculationAsync(id, 120m);

        Assert.Equal(100m, result.Net);
        Assert.Equal(20m, result.Vat);
        Assert.Equal(0.20m, result.Rate);
    }

    [Fact]
    public async Task GetVatCalculationAsync_UnregisteredSupplier_ReturnsNone()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Onboarded(vatNumber: null));

        var result = await service.GetVatCalculationAsync(id, 120m);

        Assert.Equal(120m, result.Net);
        Assert.Equal(0m, result.Vat);
        Assert.Equal(0m, result.Rate);
    }

    [Fact]
    public async Task GetVatCalculationAsync_UnknownTenant_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TenantEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetVatCalculationAsync(id, 120m));
    }

    [Fact]
    public async Task GetVatCalculationAsync_TenantWithoutCompliance_ThrowsInvalidOperation()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Bare());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetVatCalculationAsync(id, 120m));
    }

    #endregion

    #region GetTaxComplianceAsync

    [Fact]
    public async Task GetTaxComplianceAsync_OnboardedTenant_MapsAllFields()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Onboarded("GB123456789"));

        var compliance = await service.GetTaxComplianceAsync(id);

        Assert.NotNull(compliance);
        Assert.Equal("GB123456789", compliance!.VatNumber);
        Assert.Equal("SID000001", compliance.SellerIdentifier);
        Assert.Equal("GB00BANK00000000000001", compliance.BankReference);
        Assert.Equal("1 Main St", compliance.RegisteredAddress.Line1);
        Assert.Equal("Floor 2", compliance.RegisteredAddress.Line2);
        Assert.Equal("London", compliance.RegisteredAddress.City);
        Assert.Equal("EC1A 1AA", compliance.RegisteredAddress.Postcode);
        Assert.Equal("United Kingdom", compliance.RegisteredAddress.Country);
    }

    [Fact]
    public async Task GetTaxComplianceAsync_UnknownTenant_ReturnsNull()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TenantEntity?)null);

        Assert.Null(await service.GetTaxComplianceAsync(id));
    }

    [Fact]
    public async Task GetTaxComplianceAsync_TenantWithoutCompliance_ReturnsNull()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Bare());

        Assert.Null(await service.GetTaxComplianceAsync(id));
    }

    #endregion

    #region IsTaxComplianceCompleteAsync

    [Fact]
    public async Task IsTaxComplianceCompleteAsync_OnboardedTenant_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Onboarded("GB123456789"));

        Assert.True(await service.IsTaxComplianceCompleteAsync(id));
    }

    [Fact]
    public async Task IsTaxComplianceCompleteAsync_TenantWithoutCompliance_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Bare());

        Assert.False(await service.IsTaxComplianceCompleteAsync(id));
    }

    [Fact]
    public async Task IsTaxComplianceCompleteAsync_UnknownTenant_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TenantEntity?)null);

        Assert.False(await service.IsTaxComplianceCompleteAsync(id));
    }

    #endregion
}
