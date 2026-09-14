using System.Collections.Frozen;

namespace Concertable.B2B.Privacy.Domain.Lifecycle;

/// <summary>The subject-erasure lifecycle's legal edges. Fail-closed: an unlisted (state, trigger) pair is an
/// <see cref="ErasureTransitionError.InvalidTransition"/>, never a silent no-op.</summary>
internal sealed class ErasureStateMachine
{
    private static readonly FrozenDictionary<(ErasureState, ErasureTrigger), ErasureState> Transitions =
        new Dictionary<(ErasureState, ErasureTrigger), ErasureState>
        {
            [(ErasureState.Requested, ErasureTrigger.Begin)] = ErasureState.InProgress,
            [(ErasureState.Requested, ErasureTrigger.Defer)] = ErasureState.Deferred,
            [(ErasureState.Deferred, ErasureTrigger.Begin)] = ErasureState.InProgress,
            // A still-obligated subject re-deferred by the sweep, and a run resumed after a crash part-way
            // through the cross-module fan-out, are both legal self-edges: erasure is re-drivable, not one-shot.
            [(ErasureState.Deferred, ErasureTrigger.Defer)] = ErasureState.Deferred,
            [(ErasureState.InProgress, ErasureTrigger.Begin)] = ErasureState.InProgress,
            [(ErasureState.InProgress, ErasureTrigger.Complete)] = ErasureState.Completed,
            [(ErasureState.InProgress, ErasureTrigger.Fail)] = ErasureState.Failed,
        }.ToFrozenDictionary();

    public Result<ErasureState, ErasureTransitionError> Next(ErasureState current, ErasureTrigger trigger) =>
        Transitions.TryGetValue((current, trigger), out var next)
            ? next
            : new ErasureTransitionError.InvalidTransition(current, trigger);
}
