using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingRepository : IIdRepository<BookingEntity>
{
    Task<BookingEntity?> GetByApplicationIdAsync(int applicationId);
    Task<BookingEntity?> GetByConcertIdAsync(int concertId);
    Task<int?> GetContractIdByIdAsync(int bookingId);
}
