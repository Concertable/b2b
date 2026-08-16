using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateVenueError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant =>
            ErrorDefinition.Forbidden<NoActiveTenant>(
                "No active organization was selected."),
        ActiveTenantAlreadyHasVenue =>
            ErrorDefinition.Conflict<ActiveTenantAlreadyHasVenue>(
                "The active organization already has a venue."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The venue is invalid.",
                errors)
    };

    [ErrorCode("venue.create_forbidden")]
    public partial record NoActiveTenant;

    [ErrorCode("venue.create.active_tenant_already_has_venue")]
    public partial record ActiveTenantAlreadyHasVenue;

    public partial record Invalid(ValidationErrors Errors);
}
