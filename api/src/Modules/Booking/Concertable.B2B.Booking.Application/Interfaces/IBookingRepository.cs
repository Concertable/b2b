using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IBookingRepository : IRepository<BookingEntity, int>
{
    Task<BookingEntity?> GetWithGrantsByIdAsync(int id, CancellationToken ct = default);
    Task<BookingEntity?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<IReadOnlyList<BookingEntity>> GetByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default);
    Task<BookingEntity?> GetByOperationIdAsync(
        Guid operationId,
        CancellationToken ct = default);
    Task<int?> GetApplicationIdByIdAsync(
        int bookingId,
        CancellationToken ct = default);
    Task<BookingState?> GetStateByIdAsync(
        int bookingId,
        CancellationToken ct = default);
    Task<int> GetAwaitingCheckoutCountByArtistTenantIdAsync(
        Guid artistTenantId,
        DateTime now,
        CancellationToken ct = default);
}
