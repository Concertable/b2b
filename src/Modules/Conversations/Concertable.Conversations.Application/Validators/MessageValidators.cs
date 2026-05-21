using Concertable.Conversations.Application.Requests;
using FluentValidation;

namespace Concertable.Conversations.Application.Validators;

internal class MarkMessagesReadRequestValidator : AbstractValidator<MarkMessagesReadRequest>
{
    public MarkMessagesReadRequestValidator()
    {
        RuleFor(x => x.MessageIds).NotEmpty().WithMessage("Require one MessageId minimum.");
    }
}
