using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingAgreementRepository : IVenueArtistTenantScopedRepository<BookingAgreementEntity>
{
    /* Tenant-filtered: resolves the agreement for the caller's own application, or null when the
       caller is not a party (the two-party filter hides it) — same 404-not-403 stance as reading
       the application itself. */
    Task<BookingAgreementEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default);

    /* Unfiltered: the background PDF generator runs with no tenant context, so it must bypass the
       two-party filter to find the just-created agreement by its booking. */
    Task<BookingAgreementEntity?> GetByBookingIdIgnoringTenantAsync(int bookingId, CancellationToken ct = default);

    Task<int?> GetIdByApplicationIdAsync(int applicationId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, int>> GetIdsByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken ct = default);
}
