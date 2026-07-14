using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingService
{
    Task<StandardBookingDto> CreateStandardAsync(int applicationId, DealType dealType);
    Task<DeferredBookingDto> CreateDeferredAsync(int applicationId, DealType dealType, string paymentMethodId);
    Task<BookingSettlement> GetSettlementByConcertIdAsync(int concertId);
}
