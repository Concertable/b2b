using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IBookingRepository : IVenueArtistTenantScopedRepository<BookingEntity>
{
    Task<BookingEntity?> GetByApplicationIdAsync(
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
}
