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

internal sealed class ComplianceDtoValidator : AbstractValidator<ComplianceDto>
{
    public ComplianceDtoValidator()
    {
        When(x => x.VatRegistered, () =>
        {
            RuleFor(x => x.VatNumber)
                .NotEmpty()
                .MaximumLength(20)
                .Matches(@"^(GB)?(\d{9}|\d{12})$")
                .WithMessage("VAT number must be 9 or 12 digits, optionally prefixed with GB.");
        });

        When(x => !x.VatRegistered, () =>
        {
            RuleFor(x => x.VatNumber)
                .Empty()
                .WithMessage("VAT number must be empty when not VAT-registered.");
        });

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
