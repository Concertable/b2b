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
}
