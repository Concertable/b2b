using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class StateMachineTests
{
    [Fact]
    public void Transition_CoversEveryStateAndTrigger()
    {
        var expected = new Dictionary<(State, Trigger), State>
        {
            [(State.Draft, Trigger.Post)] = State.Posted,
            [(State.Posted, Trigger.Post)] = State.Posted,
            [(State.Draft, Trigger.BeginCancellation)] = State.CancellationPending,
            [(State.Posted, Trigger.BeginCancellation)] = State.CancellationPending,
            [(State.CancellationFailed, Trigger.BeginCancellation)] = State.CancellationPending,
            [(State.CancellationPending, Trigger.RecordCancellationFailure)] = State.CancellationFailed,
            [(State.CancellationPending, Trigger.Cancel)] = State.Cancelled,
            [(State.CancellationFailed, Trigger.Cancel)] = State.Cancelled,
            [(State.Draft, Trigger.BeginSettlement)] = State.AwaitingSettlement,
            [(State.Posted, Trigger.BeginSettlement)] = State.AwaitingSettlement,
            [(State.SettlementFailed, Trigger.BeginSettlement)] = State.AwaitingSettlement,
            [(State.AwaitingSettlement, Trigger.RecordSettlementFailure)] = State.SettlementFailed,
            [(State.Draft, Trigger.CompleteSettlement)] = State.Complete,
            [(State.Posted, Trigger.CompleteSettlement)] = State.Complete,
            [(State.AwaitingSettlement, Trigger.CompleteSettlement)] = State.Complete,
            [(State.SettlementFailed, Trigger.CompleteSettlement)] = State.Complete
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
