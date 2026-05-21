namespace Concertable.Concert.Application.Interfaces;

internal interface IBookingRepository : IIdRepository<BookingEntity>
{
    Task<BookingEntity?> GetByApplicationIdAsync(int applicationId);
    Task<BookingEntity?> GetByConcertIdAsync(int concertId);
    Task<int?> GetContractIdByIdAsync(int bookingId);
}
