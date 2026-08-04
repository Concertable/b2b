using Concertable.Kernel.Errors;

namespace Concertable.B2B.Venue.Application.Errors;

internal sealed record VenueError(ErrorDefinition Definition) : IError
{
    internal static VenueError NotFound(int venueId) =>
        new(ErrorDefinition.NotFound(
            "venue.get.not_found",
            $"Venue {venueId} was not found."));

    internal static readonly VenueError CurrentTenantNotFound = new(
        ErrorDefinition.NotFound(
            "venue.get.current_tenant_not_found",
            "No venue was found for the current tenant."));
}
