using Concertable.Kernel.Errors;

namespace Concertable.B2B.Venue.Api.Errors;

internal sealed record GetVenueError : IError
{
    private GetVenueError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static GetVenueError NotFound(int venueId) =>
        new(ErrorDefinition.NotFound(
            "venue.get.not_found",
            $"Venue {venueId} was not found."));
}
