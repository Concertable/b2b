namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingExistence
{
    Task<bool> ExistsAsync(int bookingId);
}
