using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IRejectExecutor
{
    Task<UnitResult<LifecycleTransitionError>> RejectAsync(int applicationId);
}
