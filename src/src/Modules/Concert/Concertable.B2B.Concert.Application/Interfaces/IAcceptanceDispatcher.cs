namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IAcceptanceDispatcher
{
    Task AcceptAsync(int applicationId, string? paymentMethodId);
}
