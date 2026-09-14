using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ConcertStateMachineTests
{
    [Fact]
    public void Transition_CoversEveryStateAndTrigger()
    {
        var expected = new Dictionary<(ConcertState, ConcertTrigger), ConcertState>
        {
            [(ConcertState.Draft, ConcertTrigger.Post)] = ConcertState.Posted,
            [(ConcertState.Posted, ConcertTrigger.Post)] = ConcertState.Posted,
            [(ConcertState.Draft, ConcertTrigger.BeginCancellation)] = ConcertState.CancellationPending,
            [(ConcertState.Posted, ConcertTrigger.BeginCancellation)] = ConcertState.CancellationPending,
            [(ConcertState.CancellationFailed, ConcertTrigger.BeginCancellation)] = ConcertState.CancellationPending,
            [(ConcertState.CancellationPending, ConcertTrigger.RecordCancellationFailure)] = ConcertState.CancellationFailed,
            [(ConcertState.CancellationPending, ConcertTrigger.Cancel)] = ConcertState.Cancelled,
            [(ConcertState.CancellationFailed, ConcertTrigger.Cancel)] = ConcertState.Cancelled,
            [(ConcertState.Draft, ConcertTrigger.BeginSettlement)] = ConcertState.AwaitingSettlement,
            [(ConcertState.Posted, ConcertTrigger.BeginSettlement)] = ConcertState.AwaitingSettlement,
            [(ConcertState.SettlementFailed, ConcertTrigger.BeginSettlement)] = ConcertState.AwaitingSettlement,
            [(ConcertState.AwaitingSettlement, ConcertTrigger.RecordSettlementFailure)] = ConcertState.SettlementFailed,
            [(ConcertState.Draft, ConcertTrigger.CompleteSettlement)] = ConcertState.Complete,
            [(ConcertState.Posted, ConcertTrigger.CompleteSettlement)] = ConcertState.Complete,
            [(ConcertState.AwaitingSettlement, ConcertTrigger.CompleteSettlement)] = ConcertState.Complete,
            [(ConcertState.SettlementFailed, ConcertTrigger.CompleteSettlement)] = ConcertState.Complete
        };
        var machine = new ConcertStateMachine();

        foreach (var state in Enum.GetValues<ConcertState>())
        foreach (var trigger in Enum.GetValues<ConcertTrigger>())
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
