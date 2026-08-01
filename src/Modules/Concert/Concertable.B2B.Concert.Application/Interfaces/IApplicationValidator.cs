using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Application.Errors;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationValidator
{
    Task<UnitResult<ValidationErrors>> CanApplyAsync(OpportunityEntity opportunity, int artistId);
    Task<UnitResult<ApplicationEligibilityError>> CanApplyAsync(int opportunityId);
    Task<UnitResult<ValidationErrors>> CanAcceptAsync(OpportunityEntity opportunity, ApplicationEntity application);
    Task<UnitResult<ApplicationEligibilityError>> CanAcceptAsync(int applicationId);
}
