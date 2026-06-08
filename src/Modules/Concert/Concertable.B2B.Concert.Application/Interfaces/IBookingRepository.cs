using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingRepository : IRepository<BookingEntity>
{
    Task<BookingEntity?> GetByApplicationIdAsync(int applicationId);
    Task<BookingEntity?> GetForSettlementByConcertIdAsync(int concertId);
    Task<int?> GetIdByConcertIdAsync(int concertId);
    Task<int?> GetApplicationIdByIdAsync(int bookingId);
    Task<int?> GetContractIdByIdAsync(int bookingId);
}
