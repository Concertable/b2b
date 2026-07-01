using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingService
{
    Task<StandardBookingDto> CreateStandardAsync(int applicationId, ContractType contractType);
    Task<DeferredBookingDto> CreateDeferredAsync(int applicationId, ContractType contractType, string paymentMethodId);
    Task<BookingSettlement> GetSettlementByConcertIdAsync(int concertId);
}
