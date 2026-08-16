using Concertable.B2B.User.Application.Errors;
using Reunion.Errors;

namespace Concertable.B2B.User.UnitTests;

public sealed class UserErrorTests
{
    [Fact]
    public void Definition_UserNotFound_ReturnsStableDefinition()
    {
        var definition = new SaveLocationError.UserNotFound().Definition;

        Assert.Equal("user.location_unauthenticated", definition.Code);
        Assert.Equal("The current user was not found.", definition.Message);
        Assert.Equal(ErrorKind.Unauthenticated, definition.Kind);
    }
}
