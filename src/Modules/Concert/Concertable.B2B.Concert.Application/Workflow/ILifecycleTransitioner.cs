using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Application.Workflow;

internal delegate Task TransitionEffect(ApplicationEntity application);
internal delegate Task<UnitResult<TError>> TransitionEffect<TError>(ApplicationEntity application)
    where TError : notnull;

internal interface ILifecycleTransitioner
{
    Task<Result<ApplicationEntity, LifecycleTransitionError>> TransitionAsync(
        int applicationId,
        Trigger trigger,
        TransitionEffect? effect = null,
        CancellationToken ct = default);

    Task<Result<ApplicationEntity, TError>> TransitionAsync<TError>(
        int applicationId,
        Trigger trigger,
        Func<LifecycleTransitionError, TError> mapTransitionError,
        TransitionEffect<TError> effect,
        CancellationToken ct = default)
        where TError : notnull;
}
