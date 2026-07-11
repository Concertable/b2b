using Concertable.B2B.Concert.Api.Requests;
using Concertable.B2B.Concert.Application.Requests;
using FluentValidation;

namespace Concertable.B2B.Concert.Api.Validators;

internal sealed class ESignatureRequestValidator : AbstractValidator<ESignatureRequest>
{
    public ESignatureRequestValidator()
    {
        RuleFor(x => x.SignatoryName).NotEmpty().WithMessage("You must sign by entering your full name");
    }
}

internal sealed class ApplyRequestValidator : AbstractValidator<ApplyRequest>
{
    public ApplyRequestValidator()
    {
        RuleFor(x => x.ESignature).NotNull().WithMessage("You must sign the booking agreement");
        RuleFor(x => x.ESignature).SetValidator(new ESignatureRequestValidator()).When(x => x.ESignature is not null);
    }
}

internal sealed class AcceptRequestValidator : AbstractValidator<AcceptRequest>
{
    public AcceptRequestValidator()
    {
        RuleFor(x => x.ESignature).NotNull().WithMessage("You must sign the booking agreement");
        RuleFor(x => x.ESignature).SetValidator(new ESignatureRequestValidator()).When(x => x.ESignature is not null);
    }
}
