using Concertable.B2B.Conversations.Application.Requests;
using FluentValidation;

namespace Concertable.B2B.Conversations.Application.Validators;

internal sealed class MarkMessagesReadRequestValidator : AbstractValidator<MarkMessagesReadRequest>
{
    public MarkMessagesReadRequestValidator()
    {
        RuleFor(x => x.MessageIds).NotEmpty().WithMessage("Require one MessageId minimum.");
    }
}
