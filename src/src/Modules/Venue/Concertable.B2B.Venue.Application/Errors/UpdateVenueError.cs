using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union]
internal partial record UpdateVenueError : IError
{
    partial record VenueNotFound(int VenueId);

    public static UpdateVenueError NotFound(int venueId) => new VenueNotFound(venueId);

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "venue.update_not_found",
            $"Venue {error.VenueId} was not found."));
}
