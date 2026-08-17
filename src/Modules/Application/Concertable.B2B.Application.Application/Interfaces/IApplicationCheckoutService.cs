using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Responses;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationCheckoutService
{
    Task<Result<Checkout, ApplicationEligibilityError>> CreateApplyCheckoutAsync(int opportunityId);
    Task<Checkout> CreateAcceptCheckoutAsync(int applicationId);
}
