namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IVerifyDispatcher
{
    Task VerifyAsync(int applicationId);
}
