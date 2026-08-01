using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union]
internal partial record ApproveVenueError : IError
{
    partial record VenueNotFound(int VenueId);

    public static ApproveVenueError NotFound(int venueId) => new VenueNotFound(venueId);

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "venue.approve_not_found",
            $"Venue {error.VenueId} was not found."));
}
