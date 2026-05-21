namespace Concertable.Concert.Application.Interfaces;

internal interface IVerifyDispatcher
{
    Task VerifyAsync(int applicationId);
}
