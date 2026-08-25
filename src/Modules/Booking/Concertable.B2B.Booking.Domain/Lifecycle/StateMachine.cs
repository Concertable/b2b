using Concertable.Kernel;
using Reunion;

namespace Concertable.B2B.Booking.Domain.Lifecycle;

internal sealed class StateMachine : IStateMachine<State, Trigger>
{
    private readonly IStateMachine<State, Trigger> transitions =
        new Concertable.Kernel.StateMachine<State, Trigger>(
        [
            (State.AwaitingConfirmation, Trigger.Confirm, State.Confirmed),
            (State.ConfirmationFailed, Trigger.Confirm, State.Confirmed),
            (State.AwaitingConfirmation, Trigger.RecordConfirmationFailure, State.ConfirmationFailed),
            (State.ConfirmationFailed, Trigger.RecordConfirmationFailure, State.ConfirmationFailed),
            (State.AwaitingConfirmation, Trigger.BeginCancellation, State.CancellationPending),
            (State.ConfirmationFailed, Trigger.BeginCancellation, State.CancellationPending),
            (State.CancellationFailed, Trigger.BeginCancellation, State.CancellationPending),
            (State.CancellationPending, Trigger.RecordCancellationFailure, State.CancellationFailed),
            (State.CancellationPending, Trigger.Cancel, State.Cancelled),
            (State.CancellationFailed, Trigger.Cancel, State.Cancelled)
        ]);

    public Result<State, TransitionError<State, Trigger>> Transition(State current, Trigger trigger) =>
        transitions.Transition(current, trigger);
}
