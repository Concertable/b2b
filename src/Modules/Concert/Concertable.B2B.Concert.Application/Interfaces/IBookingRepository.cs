using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingRepository : IVenueArtistTenantScopedRepository<BookingEntity>
{
    Task<BookingEntity?> GetByApplicationIdAsync(int applicationId);
    Task<BookingEntity?> GetForSettlementByConcertIdAsync(int concertId);
    Task<int?> GetIdByConcertIdAsync(int concertId);
    Task<int?> GetApplicationIdByIdAsync(int bookingId);
    Task<int?> GetContractIdByIdAsync(int bookingId);
}
