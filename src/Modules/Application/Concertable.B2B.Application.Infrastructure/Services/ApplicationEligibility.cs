using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Opportunity.Contracts;
using Reunion.Validation;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationEligibility : IApplicationEligibility
{
    private readonly IOpportunityModule opportunityModule;
    private readonly IApplicationValidator validator;

    public ApplicationEligibility(
        IOpportunityModule opportunityModule,
        IApplicationValidator validator)
    {
        this.opportunityModule = opportunityModule;
        this.validator = validator;
    }

    public Task<Result<OpportunityDto, ApplicationEligibilityError>> CanAcceptAsync(
        ApplicationEntity application,
        CancellationToken ct = default) =>
        opportunityModule.GetAsync(application.OpportunityId, ct)
            .OrFailure<OpportunityDto, ApplicationEligibilityError>(
                new ApplicationEligibilityError.OpportunityNotFound())
            .EnsureAsync(
                opportunity => validator.CanAcceptAsync(opportunity, application, ct),
                errors => (ApplicationEligibilityError)new ApplicationEligibilityError.Invalid(errors));
}
