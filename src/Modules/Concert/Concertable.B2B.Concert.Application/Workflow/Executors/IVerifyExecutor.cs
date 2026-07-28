namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IVerifyExecutor
{
    Task ExecuteAsync(int applicationId, string transactionId);
    Task ExecuteFailedAsync(int applicationId, string venueManagerId, string? failureMessage);
    Task ConvergeAfterAcceptAsync(int applicationId);
}
