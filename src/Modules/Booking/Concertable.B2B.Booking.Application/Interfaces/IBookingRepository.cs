using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IBookingRepository : IVenueArtistTenantScopedRepository<BookingEntity>
{
    /// <summary>
    /// Saves pending changes. An EF update failure returns <see langword="false"/> and clears the complete
    /// tracked unit of work; every other failure propagates.
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct = default);

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
    Task<int> GetAwaitingCheckoutCountByArtistTenantIdAsync(
        Guid artistTenantId,
        DateTime now,
        CancellationToken ct = default);
}
