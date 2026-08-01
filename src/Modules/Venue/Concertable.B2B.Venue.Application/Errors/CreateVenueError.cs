using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union]
internal partial record CreateVenueError : IError
{
    partial record NoActiveTenant;

    public static CreateVenueError Forbidden() => new NoActiveTenant();

    public ErrorDefinition Definition => ErrorDefinition.Forbidden(
        "venue.create_forbidden",
        "No active organization was found for the current user.");
}
