namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISettlementDispatcher
{
    Task SettleAsync(int bookingId);
}
