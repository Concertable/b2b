using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IBookingPrivilegedRepository
{
    Task<BookingEntity?> GetByIdAsync(int bookingId, CancellationToken ct = default);
    Task<BookingEntity?> GetWithContractByIdAsync(int bookingId, CancellationToken ct = default);
    Task<int?> GetIdByApplicationIdAsync(int applicationId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
