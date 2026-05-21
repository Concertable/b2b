using Concertable.Concert.Application.DTOs;
using FluentValidation;

namespace Concertable.Concert.Application.Validators;

internal class OpportunityDtoValidator : AbstractValidator<OpportunityDto>
{
    public OpportunityDtoValidator()
    {
        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("You cannot create a Concert Opportunity in the past.");

        RuleFor(x => x.EndDate)
            .Must((dto, endDate) => (endDate - dto.StartDate).TotalHours <= 24)
            .WithMessage("EndDate can be at most 24 hours after StartDate.")
            .When(x => x.EndDate > x.StartDate);
    }
}
