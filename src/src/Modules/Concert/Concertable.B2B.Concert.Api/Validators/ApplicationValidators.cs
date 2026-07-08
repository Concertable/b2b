using Concertable.B2B.Concert.Api.Requests;
using FluentValidation;

namespace Concertable.B2B.Concert.Api.Validators;

internal sealed class ApplyRequestValidator : AbstractValidator<ApplyRequest>
{
    public ApplyRequestValidator()
    {
        RuleFor(x => x.AgreedToTerms).Equal(true).WithMessage("You must agree to the contract terms");
    }
}

internal sealed class AcceptRequestValidator : AbstractValidator<AcceptRequest>
{
    public AcceptRequestValidator()
    {
        RuleFor(x => x.AgreedToTerms).Equal(true).WithMessage("You must agree to the contract terms");
    }
}
