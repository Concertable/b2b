using Concertable.B2B.Conversations.Application.Requests;
using FluentValidation;

namespace Concertable.B2B.Conversations.Application.Validators;

internal sealed class ReportMessageRequestValidator : AbstractValidator<ReportMessageRequest>
{
    public const int MaxDetailsLength = 2000;

    public ReportMessageRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();

        // Optional on purpose: the category may say everything, and a reporting route must never be
        // harder to complete than it has to be.
        RuleFor(x => x.Details).MaximumLength(MaxDetailsLength);
    }
}
