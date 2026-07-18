using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Application.Tax;
using Concertable.B2B.Tenant.Application.Validators;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.UnitTests;

/// <summary>The org-form write validator now owns the VAT-number format check and its user-facing message (moved off
/// the domain <c>ITaxComplianceRules</c>), composed from region reference data.</summary>
public sealed class TenantValidatorsTests
{
    private static UpdateTenantRequestValidator Validator()
    {
        var options = Options.Create(new UkTaxComplianceOptions());
        return new UpdateTenantRequestValidator(new UkTaxComplianceRules(options), options);
    }

    private static UpdateTenantRequest Request(string? vatNumber) => new()
    {
        LegalName = "Acme Ltd",
        TaxCompliance = new TaxComplianceDto
        {
            VatNumber = vatNumber,
            SellerIdentifier = "SID000001",
            BankReference = "GB00BANK00000000000001",
            RegisteredAddress = new RegisteredAddressDto
            {
                Line1 = "1 Main St",
                City = "London",
                Postcode = "EC1A 1AA",
                Country = "United Kingdom",
            },
        },
    };

    [Fact]
    public void InvalidVatNumber_FailsWithTheComposedRegionMessage()
    {
        var result = Validator().Validate(Request("NOPE"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "TaxCompliance.VatNumber"
            && e.ErrorMessage == "VAT number must be 9 or 12 digits, optionally prefixed with GB.");
    }

    [Theory]
    [InlineData("GB123456789")]   // registered — valid format
    [InlineData(null)]            // unregistered — absence is valid
    public void ValidOrAbsentVatNumber_Passes(string? vatNumber) =>
        Assert.True(Validator().Validate(Request(vatNumber)).IsValid);
}
