using Concertable.Concert.Domain.Entities;

namespace Concertable.Concert.Application.Interfaces;

internal interface IBookingService
{
    Task<StandardBooking> CreateStandardAsync(int applicationId);
    Task<DeferredBooking> CreateDeferredAsync(int applicationId, string paymentMethodId);
    Task<BookingEntity> MarkAwaitingPaymentByConcertIdAsync(int concertId);
    Task<BookingEntity> CompleteByConcertIdAsync(int concertId);
    Task CompleteAsync(int bookingId);
    Task FailPaymentAsync(int bookingId, CancellationToken ct = default);
}
