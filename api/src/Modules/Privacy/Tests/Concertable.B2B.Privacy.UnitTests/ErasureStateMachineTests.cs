using Concertable.B2B.Privacy.Domain.Lifecycle;

namespace Concertable.B2B.Privacy.UnitTests;

public sealed class ErasureStateMachineTests
{
    private readonly ErasureStateMachine machine = new();

    [Theory]
    [InlineData(ErasureState.Requested, ErasureTrigger.Begin, ErasureState.InProgress)]
    [InlineData(ErasureState.Requested, ErasureTrigger.Defer, ErasureState.Deferred)]
    [InlineData(ErasureState.Deferred, ErasureTrigger.Begin, ErasureState.InProgress)]
    [InlineData(ErasureState.InProgress, ErasureTrigger.Complete, ErasureState.Completed)]
    [InlineData(ErasureState.InProgress, ErasureTrigger.Fail, ErasureState.Failed)]
    public void Next_LegalEdge_ReturnsNextState(ErasureState current, ErasureTrigger trigger, ErasureState expected)
    {
        var result = machine.Next(current, trigger);

        Assert.True(result.TryGetValue(out var next));
        Assert.Equal(expected, next);
    }

    [Theory]
    [InlineData(ErasureState.Requested, ErasureTrigger.Complete)]
    [InlineData(ErasureState.Requested, ErasureTrigger.Fail)]
    [InlineData(ErasureState.Deferred, ErasureTrigger.Defer)]
    [InlineData(ErasureState.Completed, ErasureTrigger.Begin)]
    [InlineData(ErasureState.Failed, ErasureTrigger.Complete)]
    public void Next_IllegalEdge_FailsClosedWithInvalidTransition(ErasureState current, ErasureTrigger trigger)
    {
        var result = machine.Next(current, trigger);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ErasureTransitionError.InvalidTransition>(error);
    }
}
