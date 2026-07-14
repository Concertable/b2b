using System.Collections.Frozen;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Domain.Lifecycle;

internal sealed class LifecycleStateMachine
{
    public FrozenDictionary<(LifecycleState, Trigger), LifecycleState> Transitions { get; }

    public LifecycleStateMachine(Dictionary<(LifecycleState, Trigger), LifecycleState> transitions)
    {
        Transitions = transitions.ToFrozenDictionary();
    }

    public LifecycleState Next(LifecycleState current, Trigger trigger)
        => Transitions.TryGetValue((current, trigger), out var next)
            ? next
            : throw new ConflictException($"Cannot {trigger} from {current}");
}
