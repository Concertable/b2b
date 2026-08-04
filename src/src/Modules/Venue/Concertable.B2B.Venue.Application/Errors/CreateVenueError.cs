namespace Concertable.B2B.Venue.Application.Errors;

internal sealed record CreateVenueError(ErrorDefinition Definition) : IError
{
    internal static readonly CreateVenueError Forbidden = new(
        ErrorDefinition.Forbidden(
            "venue.create_forbidden",
            "No active organization was found for the current user."));
}
