using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IContractLoader
{
    Task<IContract> LoadByApplicationIdAsync(int applicationId);
    Task<IContract> LoadByOpportunityIdAsync(int opportunityId);
    Task<IContract> LoadByBookingIdAsync(int bookingId);
    Task<IContract?> TryLoadByBookingIdAsync(int bookingId);
    Task<IContract> LoadByConcertIdAsync(int concertId);
}
