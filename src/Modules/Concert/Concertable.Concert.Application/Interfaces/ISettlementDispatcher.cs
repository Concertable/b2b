namespace Concertable.Concert.Application.Interfaces;

internal interface ISettlementDispatcher
{
    Task SettleAsync(int bookingId);
}
