using Concertable.B2B.Privacy.Domain.Lifecycle;

namespace Concertable.B2B.Privacy.UnitTests;

public sealed class ErasureStateMachineTests
{
    private readonly ErasureStateMachine machine = new();

    [Theory]
    [InlineData(ErasureState.Requested, ErasureTrigger.Begin, ErasureState.InProgress)]
    [InlineData(ErasureState.Requested, ErasureTrigger.Defer, ErasureState.Deferred)]
    [InlineData(ErasureState.Deferred, ErasureTrigger.Begin, ErasureState.InProgress)]
    [InlineData(ErasureState.Deferred, ErasureTrigger.Defer, ErasureState.Deferred)]
    [InlineData(ErasureState.InProgress, ErasureTrigger.Begin, ErasureState.InProgress)]
    [InlineData(ErasureState.InProgress, ErasureTrigger.Complete, ErasureState.Completed)]
    [InlineData(ErasureState.InProgress, ErasureTrigger.Fail, ErasureState.Failed)]
    public void Transition_LegalEdge_ReturnsNextState(ErasureState current, ErasureTrigger trigger, ErasureState expected)
    {
        var result = machine.Transition(current, trigger);

        Assert.True(result.TryGetValue(out var next));
        Assert.Equal(expected, next);
    }

    [Theory]
    [InlineData(ErasureState.Requested, ErasureTrigger.Complete)]
    [InlineData(ErasureState.Requested, ErasureTrigger.Fail)]
    [InlineData(ErasureState.Deferred, ErasureTrigger.Complete)]
    [InlineData(ErasureState.Completed, ErasureTrigger.Begin)]
    [InlineData(ErasureState.Failed, ErasureTrigger.Complete)]
    [InlineData(ErasureState.Completed, ErasureTrigger.Defer)]
    public void Transition_IllegalEdge_FailsClosed(ErasureState current, ErasureTrigger trigger)
    {
        var result = machine.Transition(current, trigger);

        Assert.True(result.TryGetError(out var error));
        Assert.NotNull(error);
    }
}
