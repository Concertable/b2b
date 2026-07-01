using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IApplyExecutor
{
    Task<ApplicationEntity> ExecuteAsync(int opportunityId, int artistId, string? paymentMethodId);
}
