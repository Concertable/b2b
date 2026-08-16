using Concertable.B2B.Conversations.Application.Errors;
using Reunion.Errors;

namespace Concertable.B2B.Conversations.UnitTests.Errors;

public sealed class ModerationErrorTests
{
    [Fact]
    public void MessageNotFound_Definition_IsStable()
    {
        var definition = new ModerationError.MessageNotFound().Definition;

        Assert.Equal("moderation.message_not_found", definition.Code);
        Assert.Equal("Message not found.", definition.Message);
        Assert.Equal(ErrorKind.NotFound, definition.Kind);
    }

    [Fact]
    public void ReportNotFound_Definition_IsStable()
    {
        var definition = new ModerationError.ReportNotFound().Definition;

        Assert.Equal("moderation.report_not_found", definition.Code);
        Assert.Equal("Report not found.", definition.Message);
        Assert.Equal(ErrorKind.NotFound, definition.Kind);
    }

    [Fact]
    public void AlreadyResolved_Definition_IsStable()
    {
        var definition = new ModerationError.AlreadyResolved().Definition;

        Assert.Equal("moderation.already_resolved", definition.Code);
        Assert.Equal("This report has already been resolved.", definition.Message);
        Assert.Equal(ErrorKind.Conflict, definition.Kind);
    }
}
