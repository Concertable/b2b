namespace Concertable.B2B.Booking.Infrastructure.Data;

internal interface IBookingReadDbContext
{
    IQueryable<BookingEntity> Bookings { get; }
    IQueryable<ContractEntity> Contracts { get; }
}
