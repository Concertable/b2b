using Concertable.B2B.Admin.Application.Requests;
using FluentValidation;

namespace Concertable.B2B.Admin.Application.Validators;

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
