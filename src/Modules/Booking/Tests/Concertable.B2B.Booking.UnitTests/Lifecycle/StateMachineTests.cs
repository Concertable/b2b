using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class StateMachineTests
{
    [Fact]
    public void Transition_CoversEveryStateAndTrigger()
    {
        var expected = new Dictionary<(State, Trigger), State>
        {
            [(State.AwaitingConfirmation, Trigger.Confirm)] = State.Confirmed,
            [(State.ConfirmationFailed, Trigger.Confirm)] = State.Confirmed,
            [(State.AwaitingConfirmation, Trigger.RecordConfirmationFailure)] = State.ConfirmationFailed,
            [(State.ConfirmationFailed, Trigger.RecordConfirmationFailure)] = State.ConfirmationFailed,
            [(State.AwaitingConfirmation, Trigger.BeginCancellation)] = State.CancellationPending,
            [(State.ConfirmationFailed, Trigger.BeginCancellation)] = State.CancellationPending,
            [(State.CancellationFailed, Trigger.BeginCancellation)] = State.CancellationPending,
            [(State.CancellationPending, Trigger.RecordCancellationFailure)] = State.CancellationFailed,
            [(State.CancellationPending, Trigger.Cancel)] = State.Cancelled,
            [(State.CancellationFailed, Trigger.Cancel)] = State.Cancelled
        };
        var machine = new StateMachine();

        foreach (var state in Enum.GetValues<State>())
        foreach (var trigger in Enum.GetValues<Trigger>())
        {
            var result = machine.Transition(state, trigger);
            if (expected.TryGetValue((state, trigger), out var next))
            {
                Assert.True(result.TryGetValue(out var actual));
                Assert.Equal(next, actual);
            }
            else
            {
                Assert.True(result.TryGetError(out var error));
                Assert.Equal(state, error.Current);
                Assert.Equal(trigger, error.Trigger);
            }
        }
    }
}
