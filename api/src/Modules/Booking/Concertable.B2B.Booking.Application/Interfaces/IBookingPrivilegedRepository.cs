using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IBookingPrivilegedRepository
{
    Task<BookingEntity?> GetByIdAsync(int bookingId, CancellationToken ct = default);
    Task<BookingEntity?> GetByIdForUpdateAsync(int bookingId, CancellationToken ct = default);
    Task<BookingEntity?> GetWithContractByIdAsync(int bookingId, CancellationToken ct = default);
    Task<BookingEntity?> GetWithContractByIdForUpdateAsync(int bookingId, CancellationToken ct = default);
    Task<int?> GetIdByApplicationIdAsync(int applicationId, CancellationToken ct = default);
    Task<BookingState?> GetStateByIdAsync(int bookingId, CancellationToken ct = default);
    Task<bool> CanCancelAsync(
        int bookingId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
