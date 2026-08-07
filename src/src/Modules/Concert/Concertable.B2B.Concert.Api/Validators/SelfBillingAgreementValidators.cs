using Concertable.B2B.Concert.Api.Requests;
using FluentValidation;

namespace Concertable.B2B.Concert.Api.Validators;

internal sealed class GrantSelfBillingAgreementRequestValidator : AbstractValidator<GrantSelfBillingAgreementRequest>
{
    public GrantSelfBillingAgreementRequestValidator()
    {
        RuleFor(x => x.ESignature).NotNull().WithMessage("You must sign the self-billing agreement");
        RuleFor(x => x.ESignature).SetValidator(new ESignatureRequestValidator()).When(x => x.ESignature is not null);
    }
}
