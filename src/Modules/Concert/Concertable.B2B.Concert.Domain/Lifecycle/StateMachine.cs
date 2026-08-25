using Concertable.Kernel;
using Reunion;

namespace Concertable.B2B.Concert.Domain.Lifecycle;

internal sealed class StateMachine : IStateMachine<State, Trigger>
{
    private readonly IStateMachine<State, Trigger> transitions =
        new Concertable.Kernel.StateMachine<State, Trigger>(
        [
            (State.Draft, Trigger.Post, State.Posted),
            (State.Posted, Trigger.Post, State.Posted),
            (State.Draft, Trigger.BeginCancellation, State.CancellationPending),
            (State.Posted, Trigger.BeginCancellation, State.CancellationPending),
            (State.CancellationFailed, Trigger.BeginCancellation, State.CancellationPending),
            (State.CancellationPending, Trigger.RecordCancellationFailure, State.CancellationFailed),
            (State.CancellationPending, Trigger.Cancel, State.Cancelled),
            (State.CancellationFailed, Trigger.Cancel, State.Cancelled),
            (State.Draft, Trigger.BeginSettlement, State.AwaitingSettlement),
            (State.Posted, Trigger.BeginSettlement, State.AwaitingSettlement),
            (State.SettlementFailed, Trigger.BeginSettlement, State.AwaitingSettlement),
            (State.AwaitingSettlement, Trigger.RecordSettlementFailure, State.SettlementFailed),
            (State.Draft, Trigger.CompleteSettlement, State.Complete),
            (State.Posted, Trigger.CompleteSettlement, State.Complete),
            (State.AwaitingSettlement, Trigger.CompleteSettlement, State.Complete),
            (State.SettlementFailed, Trigger.CompleteSettlement, State.Complete)
        ]);

    public Result<State, TransitionError<State, Trigger>> Transition(State current, Trigger trigger) =>
        transitions.Transition(current, trigger);
}
