using Concertable.B2B.Venue.Application.Errors;
using Reunion.Errors;

namespace Concertable.B2B.Venue.UnitTests;

public sealed class VenueErrorTests
{
    public static TheoryData<IError, string, string, ErrorKind> Cases => new()
    {
        {
            new ApproveVenueError.VenueNotFound(42),
            "venue.approve_not_found",
            "Venue 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new CreateVenueError.NoActiveTenant(),
            "venue.create_forbidden",
            "No active organization was found for the current user.",
            ErrorKind.Forbidden
        },
        {
            new UpdateVenueError.VenueNotFound(42),
            "venue.update_not_found",
            "Venue 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new VenueError.NotFound(42),
            "venue.get.not_found",
            "Venue 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new VenueError.CurrentTenantNotFound(),
            "venue.get.current_tenant_not_found",
            "No venue was found for the current tenant.",
            ErrorKind.NotFound
        }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Definition_ErrorCase_ReturnsStableDefinition(
        IError error,
        string expectedCode,
        string expectedMessage,
        ErrorKind expectedKind)
    {
        var definition = error.Definition;

        Assert.Equal(expectedCode, definition.Code);
        Assert.Equal(expectedMessage, definition.Message);
        Assert.Equal(expectedKind, definition.Kind);
    }
}
