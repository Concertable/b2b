using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Application.Tax;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.ValueObjects;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Concertable.Kernel.Errors;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Functional;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantServiceTests
{
    private readonly Mock<ITenantRepository> repository;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly TenantService service;

    public TenantServiceTests()
    {
        this.repository = new Mock<ITenantRepository>();
        this.tenantContext = new Mock<ITenantContext>();
        this.service = new TenantService(repository.Object, tenantContext.Object, new VatPolicy(new UkVatCalculator()));
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

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_NoActiveTenant_ReturnsForbiddenError()
    {
        var result = await service.UpdateAsync(null!);

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("tenant.update_forbidden", error.Definition.Code);
        Assert.Equal(ErrorKind.Forbidden, error.Definition.Kind);
    }

    [Fact]
    public async Task UpdateAsync_UnknownTenant_ReturnsNotFoundError()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync((TenantEntity?)null);

        var result = await service.UpdateAsync(null!);

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("tenant.update_not_found", error.Definition.Code);
        Assert.Equal(ErrorKind.NotFound, error.Definition.Kind);
    }

    #endregion

    #region GetVatCalculationAsync

    [Fact]
    public async Task GetVatCalculationAsync_RegisteredSupplier_DecomposesInclusiveGross()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Onboarded("GB123456789"));

        var result = await service.GetVatCalculationAsync(id, 120m);

        Assert.True(result.TryGetValue(out var calculation));
        Assert.Equal(100m, calculation.Net);
        Assert.Equal(20m, calculation.Vat);
        Assert.Equal(0.20m, calculation.Rate);
    }

    [Fact]
    public async Task GetVatCalculationAsync_UnregisteredSupplier_ReturnsNone()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Onboarded(vatNumber: null));

        var result = await service.GetVatCalculationAsync(id, 120m);

        Assert.True(result.TryGetValue(out var calculation));
        Assert.Equal(120m, calculation.Net);
        Assert.Equal(0m, calculation.Vat);
        Assert.Equal(0m, calculation.Rate);
    }

    [Fact]
    public async Task GetVatCalculationAsync_UnknownTenant_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TenantEntity?)null);

        var result = await service.GetVatCalculationAsync(id, 120m);

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("tenant.vat_tenant_not_found", error.Definition.Code);
        Assert.Equal(ErrorKind.NotFound, error.Definition.Kind);
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

        var result = await service.GetTaxComplianceAsync(id);

        Assert.True(result.TryGetValue(out var compliance));
        Assert.Equal("GB123456789", compliance.VatNumber);
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

        Assert.Equal(Option.None<TaxComplianceDto>(), await service.GetTaxComplianceAsync(id));
    }

    [Fact]
    public async Task GetTaxComplianceAsync_TenantWithoutCompliance_ReturnsNull()
    {
        var id = Guid.NewGuid();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Bare());

        Assert.Equal(Option.None<TaxComplianceDto>(), await service.GetTaxComplianceAsync(id));
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
