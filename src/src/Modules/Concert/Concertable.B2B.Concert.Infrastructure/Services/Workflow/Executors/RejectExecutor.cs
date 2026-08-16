using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class RejectExecutor : IRejectExecutor
{
    private readonly ILifecycleTransitioner transitioner;

    public RejectExecutor(ILifecycleTransitioner transitioner)
    {
        this.transitioner = transitioner;
    }

    public async Task<UnitResult<LifecycleTransitionError>> RejectAsync(int applicationId)
    {
        var result = await transitioner.TransitionAsync(applicationId, Trigger.Reject);
        return result.Match(
            _ => UnitResult.Success<LifecycleTransitionError>(),
            UnitResult.Failure);
    }
}
