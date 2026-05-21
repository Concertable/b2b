namespace Concertable.Concert.Application.Interfaces;

internal interface IAcceptanceDispatcher
{
    Task AcceptAsync(int applicationId, string? paymentMethodId);
}
