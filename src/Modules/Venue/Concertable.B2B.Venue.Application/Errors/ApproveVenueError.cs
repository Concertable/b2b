namespace Concertable.B2B.Venue.Application.Errors;

internal sealed record ApproveVenueError(ErrorDefinition Definition) : IError
{
    internal static ApproveVenueError NotFound(int venueId) =>
        new(ErrorDefinition.NotFound(
            "venue.approve_not_found",
            $"Venue {venueId} was not found."));
}
