using Concertable.Concert.Application.Responses;

namespace Concertable.Concert.Application.Interfaces;

internal interface ICheckoutDispatcher
{
    Task<Checkout> ApplyCheckoutAsync(int opportunityId);
    Task<Checkout> AcceptCheckoutAsync(int applicationId);
}
