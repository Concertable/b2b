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

        RuleFor(x => x.Compliance)
            .NotNull()
            .SetValidator(new ComplianceDtoValidator());
    }
}

// Jurisdiction-agnostic shape only. The jurisdiction-specific check (VAT-number format) is applied by
// TenantService against the loaded tenant's own Jurisdiction, via IDac7Strategy — a validator can't see it.
internal sealed class ComplianceDtoValidator : AbstractValidator<ComplianceDto>
{
    public ComplianceDtoValidator()
    {
        // VatNumber is optional (null/absent = not VAT-registered); only its length is jurisdiction-agnostic.
        // Format validity is jurisdiction-scoped and applied by TenantService via IDac7Strategy.
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
