using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Application.Tax;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.ValueObjects;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Reunion.Errors;
using Concertable.Kernel.Identity;
using Reunion;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantServiceTests
{
    private readonly Mock<ITenantRepository> repository;
    private readonly Mock<IMembershipRepository> membershipRepository;
    private readonly Mock<IInvitationRepository> invitationRepository;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly TenantService service;

    public TenantServiceTests()
    {
        this.repository = new Mock<ITenantRepository>();
        this.membershipRepository = new Mock<IMembershipRepository>();
        this.invitationRepository = new Mock<IInvitationRepository>();
        this.tenantContext = new Mock<ITenantContext>();
        this.service = new TenantService(
            repository.Object,
            membershipRepository.Object,
            invitationRepository.Object,
            tenantContext.Object,
            new VatPolicy(new UkVatCalculator()));
    }

    private static TenantEntity Bare() =>
        TenantEntity.Create("bare@test.com", Guid.NewGuid(), TenantType.Venue, DateTime.UtcNow);

    private static TenantEntity Onboarded(string? vatNumber)
    {
        var tenant = Bare();
        var compliance = RegisteredAddress
            .Create("1 Main St", "Floor 2", "London", "EC1A 1AA", "United Kingdom")
            .Bind(address => TaxCompliance.Create(
                vatNumber,
                "SID000001",
                address,
                "GB00BANK00000000000001",
                false))
            .Match(
                value => value,
                _ => throw new InvalidOperationException("Test tax compliance is invalid."));
        tenant.UpdateLegalDetails("Acme Ltd", compliance).Match(
            () => { },
            _ => throw new InvalidOperationException("Test tenant is invalid."));
        return tenant;
    }

    #region GetDetailsAsync

    [Fact]
    public async Task GetDetailsAsync_NoActiveTenant_ReturnsNone()
    {
        var result = await service.GetDetailsAsync();

        Assert.True(result.IsNone);
        repository.Verify(
            value => value.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_NoActiveTenant_ThrowsInvalidOperationException()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(null!));
        Assert.Equal("The operation requires an active tenant context.", exception.Message);
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

    [Fact]
    public async Task UpdateAsync_InvalidDomainFields_MapsStructuredFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository
            .Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Bare());
        var request = new UpdateTenantRequest
        {
            LegalName = "",
            TaxCompliance = new TaxComplianceDto
            {
                SellerIdentifier = "",
                BankReference = "",
                HoldsMusicLicence = false,
                RegisteredAddress = new RegisteredAddressDto
                {
                    Line1 = "",
                    City = "",
                    Postcode = "",
                    Country = ""
                }
            }
        };

        var result = await service.UpdateAsync(request);

        Assert.True(result.TryGetError(out var error));
        var invalid = Assert.IsType<UpdateTenantError.Invalid>(error);
        Assert.Equal(["Line1 is required."], invalid.Errors.Errors["Line1"]);
        Assert.Equal(["City is required."], invalid.Errors.Errors["City"]);
        Assert.Equal(["Postcode is required."], invalid.Errors.Errors["Postcode"]);
        Assert.Equal(["Country is required."], invalid.Errors.Errors["Country"]);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidTaxCompliance_MapsStructuredFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository
            .Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Bare());
        var request = new UpdateTenantRequest
        {
            LegalName = "Acme Ltd",
            TaxCompliance = ValidTaxCompliance() with
            {
                SellerIdentifier = "",
                BankReference = ""
            }
        };

        var result = await service.UpdateAsync(request);

        Assert.True(result.TryGetError(out var error));
        var invalid = Assert.IsType<UpdateTenantError.Invalid>(error);
        Assert.Equal(["SellerIdentifier is required."], invalid.Errors.Errors["SellerIdentifier"]);
        Assert.Equal(["BankReference is required."], invalid.Errors.Errors["BankReference"]);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MissingTaxCompliance_MapsStructuredFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository
            .Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Bare());
        var request = new UpdateTenantRequest
        {
            LegalName = "Acme Ltd",
            TaxCompliance = null!
        };

        var result = await service.UpdateAsync(request);

        Assert.True(result.TryGetError(out var error));
        var invalid = Assert.IsType<UpdateTenantError.Invalid>(error);
        Assert.Equal(["TaxCompliance is required."], invalid.Errors.Errors["TaxCompliance"]);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MissingRegisteredAddress_MapsStructuredFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository
            .Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Bare());
        var request = new UpdateTenantRequest
        {
            LegalName = "Acme Ltd",
            TaxCompliance = ValidTaxCompliance() with
            {
                RegisteredAddress = null!
            }
        };

        var result = await service.UpdateAsync(request);

        Assert.True(result.TryGetError(out var error));
        var invalid = Assert.IsType<UpdateTenantError.Invalid>(error);
        Assert.Equal(
            ["RegisteredAddress is required."],
            invalid.Errors.Errors["RegisteredAddress"]);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidLegalName_MapsStructuredFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository
            .Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Bare());
        var request = new UpdateTenantRequest
        {
            LegalName = "",
            TaxCompliance = ValidTaxCompliance()
        };

        var result = await service.UpdateAsync(request);

        Assert.True(result.TryGetError(out var error));
        var invalid = Assert.IsType<UpdateTenantError.Invalid>(error);
        Assert.Equal(["LegalName is required."], invalid.Errors.Errors["LegalName"]);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
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

    private static TaxComplianceDto ValidTaxCompliance() => new()
    {
        SellerIdentifier = "SID000001",
        BankReference = "GB00BANK00000000000001",
        HoldsMusicLicence = false,
        RegisteredAddress = new RegisteredAddressDto
        {
            Line1 = "1 Main St",
            City = "London",
            Postcode = "EC1A 1AA",
            Country = "United Kingdom"
        }
    };
}
