using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IBookingPrivilegedRepository
{
    Task<bool> ExistsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<BookingEntity?> GetByIdAsync(int bookingId, CancellationToken ct = default);
    Task<BookingEntity?> GetByIdWithRowLockAsync(int bookingId, CancellationToken ct = default);
    Task<BookingEntity?> GetWithContractByIdAsync(int bookingId, CancellationToken ct = default);
    Task<BookingEntity?> GetWithContractByIdWithRowLockAsync(int bookingId, CancellationToken ct = default);
    Task<int?> GetIdByApplicationIdAsync(int applicationId, CancellationToken ct = default);
    Task<BookingState?> GetStateByIdAsync(int bookingId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
