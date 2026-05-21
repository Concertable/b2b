namespace Concertable.Concert.Application.Workflow.Executors;

internal interface IAcceptExecutor
{
    Task ExecuteAsync(int applicationId, string? paymentMethodId);
}
