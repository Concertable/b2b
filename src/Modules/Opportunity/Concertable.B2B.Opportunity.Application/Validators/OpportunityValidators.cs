using Concertable.B2B.Opportunity.Application.Requests;
using FluentValidation;

namespace Concertable.B2B.Opportunity.Application.Validators;

internal sealed class OpportunityRequestValidator : AbstractValidator<OpportunityRequest>
{
    public OpportunityRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.StartDate)
            .GreaterThan(_ => timeProvider.GetUtcNow().UtcDateTime)
            .WithMessage("You cannot create a Concert Opportunity in the past.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be after StartDate.");

        RuleFor(x => x.EndDate)
            .Must((dto, endDate) => (endDate - dto.StartDate).TotalHours <= 24)
            .WithMessage("EndDate can be at most 24 hours after StartDate.")
            .When(x => x.EndDate > x.StartDate);
    }
}
