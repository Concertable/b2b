using Concertable.Contract.Contracts;

namespace Concertable.Concert.Application.Interfaces;

internal interface IContractLoader
{
    Task<IContract> LoadByApplicationIdAsync(int applicationId);
    Task<IContract> LoadByOpportunityIdAsync(int opportunityId);
    Task<IContract> LoadByBookingIdAsync(int bookingId);
    Task<IContract?> TryLoadByBookingIdAsync(int bookingId);
    Task<IContract> LoadByConcertIdAsync(int concertId);
}
