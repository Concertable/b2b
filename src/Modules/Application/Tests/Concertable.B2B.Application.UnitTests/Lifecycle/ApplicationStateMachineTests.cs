using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationStateMachineTests
{
    [Fact]
    public void Transition_CoversEveryStateAndTrigger()
    {
        var expected = new Dictionary<(ApplicationState, ApplicationTrigger), ApplicationState>
        {
            [(ApplicationState.Applied, ApplicationTrigger.Accept)] = ApplicationState.Accepted,
            [(ApplicationState.Applied, ApplicationTrigger.Reject)] = ApplicationState.Rejected,
            [(ApplicationState.Applied, ApplicationTrigger.Withdraw)] = ApplicationState.Withdrawn,
            [(ApplicationState.Applied, ApplicationTrigger.Cancel)] = ApplicationState.Cancelled
        };
        var machine = new ApplicationStateMachine();

        foreach (var state in Enum.GetValues<ApplicationState>())
        foreach (var trigger in Enum.GetValues<ApplicationTrigger>())
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
