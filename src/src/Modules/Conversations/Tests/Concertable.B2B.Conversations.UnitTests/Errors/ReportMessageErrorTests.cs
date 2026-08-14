using Concertable.B2B.Conversations.Application.Errors;
using Reunion.Errors;

namespace Concertable.B2B.Conversations.UnitTests.Errors;

public sealed class ReportMessageErrorTests
{
    [Fact]
    public void MessageNotFound_Definition_IsStable()
    {
        var definition = new ReportMessageError.MessageNotFound().Definition;

        Assert.Equal("report.message_not_found", definition.Code);
        Assert.Equal("Message not found.", definition.Message);
        Assert.Equal(ErrorKind.NotFound, definition.Kind);
    }

    [Fact]
    public void Invalid_Definition_IsStableAndPreservesTheFields()
    {
        var errors = new ValidationErrors([new("details", "Details are too long.")]);

        var definition = new ReportMessageError.Invalid(errors).Definition;

        Assert.Equal("report.message_invalid", definition.Code);
        Assert.Equal("The report is invalid.", definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        var validation = Assert.IsType<ValidationError>(definition);
        Assert.Equal(errors, validation.Errors);
    }
}
