namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IVerifyDispatcher
{
    Task VerifySucceededAsync(int applicationId, string transactionId);
    Task VerifyFailedAsync(int applicationId, string venueManagerId, string? failureMessage);
    Task ConvergeAfterAcceptAsync(int applicationId);
}
