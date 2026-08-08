using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingService
{
    Task<StandardBookingDto> CreateStandardAsync(ApplicationEntity application);
    Task<DeferredBookingDto> CreateDeferredAsync(ApplicationEntity application, string paymentMethodId);
    Task<BookingSettlement> GetSettlementByConcertIdAsync(int concertId);
}
