using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.Kernel;
using Reunion.Errors;

namespace Concertable.B2B.Application.UnitTests;

public sealed class CancelApplicationErrorTests
{
    [Fact]
    public void Definition_ApplicationNotFound_ReturnsNotFoundContract()
    {
        var error = new CancelApplicationError.ApplicationNotFound(42);

        var definition = error.Definition;

        Assert.Equal("application.cancel.not_found", definition.Code);
        Assert.Equal("Application 42 was not found.", definition.Message);
        Assert.Equal(ErrorKind.NotFound, definition.Kind);
    }

    [Fact]
    public void Definition_InvalidTransition_ReturnsConflictContract()
    {
        var transition = new TransitionError<State, Trigger>(State.Accepted, Trigger.Cancel);
        var error = new CancelApplicationError.InvalidTransition(transition);

        var definition = error.Definition;

        Assert.Equal("application.cancel.invalid_state", definition.Code);
        Assert.Equal("Cannot cancel an application from Accepted.", definition.Message);
        Assert.Equal(ErrorKind.Conflict, definition.Kind);
    }
}
