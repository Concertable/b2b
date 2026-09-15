using Concertable.B2B.Privacy.Domain.Lifecycle;

namespace Concertable.B2B.Privacy.UnitTests;

public sealed class ErasureTransitionErrorTests
{
    [Fact]
    public void InvalidTransition_Definition_HasStableCodeAndDescribesTheEdge()
    {
        ErasureTransitionError error = new ErasureTransitionError.InvalidTransition(ErasureState.Completed, ErasureTrigger.Begin);

        var definition = error.Definition;

        Assert.Equal("privacy.erasure.invalid_transition", definition.Code);
        Assert.Contains("Completed", definition.Message);
        Assert.Contains("Begin", definition.Message);
    }
}
