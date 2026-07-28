using Concertable.B2B.Concert.Application.Workflow.Executors;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class VerifyDispatcher : IVerifyDispatcher
{
    private readonly IVerifyExecutor executor;

    public VerifyDispatcher(IVerifyExecutor executor)
    {
        this.executor = executor;
    }

    public Task VerifySucceededAsync(int applicationId, string transactionId)
        => executor.ExecuteAsync(applicationId, transactionId);

    public Task VerifyFailedAsync(int applicationId, string venueManagerId, string? failureMessage)
        => executor.ExecuteFailedAsync(applicationId, venueManagerId, failureMessage);

    public Task ConvergeAfterAcceptAsync(int applicationId)
        => executor.ConvergeAfterAcceptAsync(applicationId);
}
