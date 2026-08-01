using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingRepository : IVenueArtistTenantScopedRepository<BookingEntity>
{
    Task<BookingEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default);
    Task<BookingEntity?> GetForSettlementByConcertIdAsync(int concertId);
    Task<int?> GetIdByConcertIdAsync(int concertId);
    Task<int?> GetApplicationIdByIdAsync(int bookingId, CancellationToken ct = default);
    Task<int?> GetDealIdByIdAsync(int bookingId);
}
