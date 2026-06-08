namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISettlementDispatcher
{
    Task SucceededAsync(int bookingId);
    Task FailedAsync(int bookingId);
}
