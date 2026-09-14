using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Opportunity.Contracts;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationEligibility
{
    Task<Result<OpportunityDto, ApplicationEligibilityError>> CanAcceptAsync(
        ApplicationEntity application,
        CancellationToken ct = default);
}
