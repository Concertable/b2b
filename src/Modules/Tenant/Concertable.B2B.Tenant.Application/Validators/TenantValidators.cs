using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;
using FluentValidation;

namespace Concertable.B2B.Tenant.Application.Validators;

internal sealed class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.LegalName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TaxCompliance)
            .NotNull()
            .SetValidator(new TaxComplianceDtoValidator());
    }
}

// Region-agnostic shape only. The region-specific check (VAT-number format) is applied by
// TenantService via ITaxComplianceRules (the deployment's region rules) — a validator can't see it.
internal sealed class TaxComplianceDtoValidator : AbstractValidator<TaxComplianceDto>
{
    public TaxComplianceDtoValidator()
    {
        // VatNumber is optional (null/absent = not VAT-registered); only its length is region-agnostic.
        // Format validity is region-specific and applied by TenantService via ITaxComplianceRules.
        RuleFor(x => x.VatNumber)
            .MaximumLength(20);

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
