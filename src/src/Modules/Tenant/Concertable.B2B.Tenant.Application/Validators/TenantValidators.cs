using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Application.Tax;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.Application.Validators;

internal sealed class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator(ITaxComplianceRules taxRules, IOptions<UkTaxComplianceOptions> taxOptions)
    {
        RuleFor(x => x.LegalName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TaxCompliance)
            .NotNull()
            .SetValidator(new TaxComplianceDtoValidator(taxRules, taxOptions));
    }
}

internal sealed class TaxComplianceDtoValidator : AbstractValidator<TaxComplianceDto>
{
    public TaxComplianceDtoValidator(ITaxComplianceRules taxRules, IOptions<UkTaxComplianceOptions> taxOptions)
    {
        var options = taxOptions.Value;

        // VatNumber optional (blank = unregistered); when present it must match the region format — message composed here, not in the domain rules.
        RuleFor(x => x.VatNumber)
            .MaximumLength(20)
            .Must(v => string.IsNullOrWhiteSpace(v) || taxRules.IsValidVatNumber(v))
            .WithMessage($"{options.VatLabel} must be {options.VatNumberFormatHint}.");

        RuleFor(x => x.SellerIdentifier)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.BankReference)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.RegisteredAddress)
            .NotNull()
            .SetValidator(new RegisteredAddressDtoValidator());
    }
}

internal sealed class RegisteredAddressDtoValidator : AbstractValidator<RegisteredAddressDto>
{
    public RegisteredAddressDtoValidator()
    {
        RuleFor(x => x.Line1)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Line2)
            .MaximumLength(200);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Postcode)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Country)
            .NotEmpty()
            .MaximumLength(100);
    }
}
