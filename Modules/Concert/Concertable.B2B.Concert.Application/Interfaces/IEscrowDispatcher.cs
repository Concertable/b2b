namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IEscrowDispatcher
{
    Task SucceededAsync(int bookingId);
    Task FailedAsync(int bookingId);
}
