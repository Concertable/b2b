using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record VenueError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var venueId) =>
            ErrorDefinition.NotFound<NotFound>($"Venue {venueId} was not found."),
        ActiveTenantNotFound =>
            ErrorDefinition.NotFound<ActiveTenantNotFound>(
                "No venue was found for the active tenant.")
    };

    [ErrorCode("venue.get.not_found")]
    public partial record NotFound(int VenueId);

    [ErrorCode("venue.get.active_tenant_not_found")]
    public partial record ActiveTenantNotFound;
}
