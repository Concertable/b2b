using Concertable.Kernel;
using Reunion;

namespace Concertable.B2B.Application.Domain.Lifecycle;

internal sealed class StateMachine : IStateMachine<State, Trigger>
{
    private readonly IStateMachine<State, Trigger> transitions =
        new Concertable.Kernel.StateMachine<State, Trigger>(
        [
            (State.Applied, Trigger.Accept, State.Accepted),
            (State.Applied, Trigger.Reject, State.Rejected),
            (State.Applied, Trigger.Withdraw, State.Withdrawn),
            (State.Applied, Trigger.Cancel, State.Cancelled)
        ]);

    public Result<State, TransitionError<State, Trigger>> Transition(State current, Trigger trigger) =>
        transitions.Transition(current, trigger);
}
