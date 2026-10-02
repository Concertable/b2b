using Reunion.Errors;
using Concertable.B2B.Privacy.Domain.Lifecycle;

namespace Concertable.B2B.Privacy.UnitTests;

public sealed class ErasureTransitionErrorTests
{
    [Fact]
    public void InvalidTransition_Definition_PinsCodeMessageAndKind()
    {
        ErasureTransitionError error =
            new ErasureTransitionError.InvalidTransition(ErasureState.Completed, ErasureTrigger.Begin);

        var definition = error.Definition;

        Assert.Equal("privacy.erasure.invalid_state", definition.Code);
        Assert.Equal("Cannot Begin a subject-erasure request from Completed.", definition.Message);
        Assert.Equal(ErrorKind.Conflict, definition.Kind);
    }
}
