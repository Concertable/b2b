using Concertable.Customer.Application.Requests;
using FluentValidation;

namespace Concertable.Customer.Application.Validators;

internal class CreatePreferenceRequestValidator : AbstractValidator<CreatePreferenceRequest>
{
    public CreatePreferenceRequestValidator()
    {
        RuleFor(x => x.RadiusKm).GreaterThan(0);
    }
}
