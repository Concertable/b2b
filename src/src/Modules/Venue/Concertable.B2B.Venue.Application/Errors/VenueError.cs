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
        CurrentTenantNotFound =>
            ErrorDefinition.NotFound<CurrentTenantNotFound>(
                "No venue was found for the current tenant.")
    };

    [ErrorCode("venue.get.not_found")]
    public partial record NotFound(int VenueId);

    [ErrorCode("venue.get.current_tenant_not_found")]
    public partial record CurrentTenantNotFound;
}
