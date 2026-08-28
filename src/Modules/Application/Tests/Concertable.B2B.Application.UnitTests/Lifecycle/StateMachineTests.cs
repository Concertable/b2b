using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.UnitTests;

public sealed class StateMachineTests
{
    [Fact]
    public void Transition_CoversEveryStateAndTrigger()
    {
        var expected = new Dictionary<(State, Trigger), State>
        {
            [(State.Applied, Trigger.Accept)] = State.Accepted,
            [(State.Applied, Trigger.Reject)] = State.Rejected,
            [(State.Applied, Trigger.Withdraw)] = State.Withdrawn,
            [(State.Applied, Trigger.Cancel)] = State.Cancelled
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
