using Concertable.B2B.User.Application.Requests;
using FluentValidation;

namespace Concertable.B2B.User.Application.Validators;

internal sealed class CreateAdminInvitationRequestValidator : AbstractValidator<CreateAdminInvitationRequest>
{
    public CreateAdminInvitationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}
