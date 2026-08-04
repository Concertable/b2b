using Concertable.Kernel.Errors;

namespace Concertable.B2B.Venue.Application.Errors;

internal sealed record VenueError : IError
{
    private VenueError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static VenueError NotFound(int venueId) =>
        new(ErrorDefinition.NotFound(
            "venue.get.not_found",
            $"Venue {venueId} was not found."));

    internal static VenueError NotFoundForCurrentTenant() =>
        new(ErrorDefinition.NotFound(
            "venue.get.current_tenant_not_found",
            "No venue was found for the current tenant."));
}
