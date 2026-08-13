using System.Collections.Frozen;

namespace Concertable.B2B.Concert.Domain.Lifecycle;

internal sealed class LifecycleStateMachine
{
    public FrozenDictionary<(LifecycleState, Trigger), LifecycleState> Transitions { get; }

    public LifecycleStateMachine(Dictionary<(LifecycleState, Trigger), LifecycleState> transitions)
    {
        Transitions = transitions.ToFrozenDictionary();
    }

    public Result<LifecycleState, LifecycleTransitionError> Next(LifecycleState current, Trigger trigger)
        => Transitions.TryGetValue((current, trigger), out var next)
            ? next
            : new LifecycleTransitionError.InvalidTransition(current, trigger);
}
