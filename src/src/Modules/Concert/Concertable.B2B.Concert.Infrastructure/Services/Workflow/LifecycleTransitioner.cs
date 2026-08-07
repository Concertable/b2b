using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class LifecycleTransitioner : ILifecycleTransitioner
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IConcertStateMachineRegistry machines;

    public LifecycleTransitioner(
        IApplicationRepository applicationRepository,
        IConcertStateMachineRegistry machines)
    {
        this.applicationRepository = applicationRepository;
        this.machines = machines;
    }

    public async Task<Result<ApplicationEntity, LifecycleTransitionError>> TransitionAsync(
        int applicationId,
        Trigger trigger,
        TransitionEffect? effect = null,
        CancellationToken ct = default)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId, ct);
        if (application is null)
            return Result.Failure<ApplicationEntity, LifecycleTransitionError>(
                new LifecycleTransitionError.ApplicationNotFound(applicationId));

        var machine = machines.Get(application.DealType);
        var transition = machine.Next(application.State, trigger);
        if (transition.TryGetError(out var error))
            return Result.Failure<ApplicationEntity, LifecycleTransitionError>(error);

        transition.TryGetValue(out var next);

        if (effect is not null)
            await effect(application);

        application.Transition(next);
        await applicationRepository.SaveChangesAsync(ct);
        return Result.Success<ApplicationEntity, LifecycleTransitionError>(application);
    }
}
