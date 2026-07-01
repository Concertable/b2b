using Concertable.B2B.Concert.Application.Responses;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ICheckoutDispatcher
{
    Task<Checkout> ApplyCheckoutAsync(int opportunityId);
    Task<Checkout> AcceptCheckoutAsync(int applicationId);
}
