namespace Concertable.B2B.Privacy.Domain.Lifecycle;

internal sealed class ErasureStateMachine() : Concertable.Kernel.StateMachine<ErasureState, ErasureTrigger>(
[
    (ErasureState.Requested, ErasureTrigger.Begin, ErasureState.InProgress),
    (ErasureState.Requested, ErasureTrigger.Defer, ErasureState.Deferred),
    (ErasureState.Deferred, ErasureTrigger.Begin, ErasureState.InProgress),
    (ErasureState.Deferred, ErasureTrigger.Defer, ErasureState.Deferred),
    (ErasureState.InProgress, ErasureTrigger.Begin, ErasureState.InProgress),
    (ErasureState.InProgress, ErasureTrigger.Defer, ErasureState.Deferred),
    (ErasureState.InProgress, ErasureTrigger.Complete, ErasureState.Completed),
    (ErasureState.InProgress, ErasureTrigger.Fail, ErasureState.Failed),
    (ErasureState.Failed, ErasureTrigger.Begin, ErasureState.InProgress),
    (ErasureState.Failed, ErasureTrigger.Defer, ErasureState.Deferred)
]);
