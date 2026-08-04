namespace Concertable.B2B.Venue.Application.Errors;

internal sealed record UpdateVenueError(ErrorDefinition Definition) : IError
{
    internal static UpdateVenueError NotFound(int venueId) =>
        new(ErrorDefinition.NotFound(
            "venue.update_not_found",
            $"Venue {venueId} was not found."));
}
