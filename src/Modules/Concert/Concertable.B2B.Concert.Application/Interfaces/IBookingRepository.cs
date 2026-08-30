using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingRepository : IVenueArtistTenantScopedRepository<BookingEntity>
{
    Task<BookingEntity?> GetWithApplicationAndConcertByIdAsync(int id, CancellationToken ct = default);
    Task<BookingEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default);
    Task<BookingEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default);
    Task<BookingEntity?> GetWithApplicationByConcertIdAsync(int concertId);
    Task<int?> GetIdByConcertIdAsync(int concertId);
    Task<int?> GetApplicationIdByIdAsync(int bookingId, CancellationToken ct = default);
    Task<int?> GetDealIdByIdAsync(int bookingId);
}
