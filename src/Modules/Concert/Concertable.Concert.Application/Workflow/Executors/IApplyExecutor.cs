namespace Concertable.Concert.Application.Workflow.Executors;

internal interface IApplyExecutor
{
    Task<ApplicationEntity> ExecuteAsync(int opportunityId, int artistId, string? paymentMethodId);
}
