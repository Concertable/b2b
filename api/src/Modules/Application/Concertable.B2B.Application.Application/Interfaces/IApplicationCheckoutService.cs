using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Responses;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationCheckoutService
{
    Task<Result<Checkout, ApplicationCheckoutError>> CreateApplyCheckoutAsync(int opportunityId);
    Task<Result<Checkout, ApplicationCheckoutError>> CreateAcceptCheckoutAsync(int applicationId);
}
