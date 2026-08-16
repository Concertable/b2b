using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateVenueError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ActiveTenantNotFound =>
            ErrorDefinition.NotFound<ActiveTenantNotFound>(
                "No venue was found for the active tenant."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The venue update is invalid.",
                errors)
    };

    [ErrorCode("venue.update_not_found")]
    public partial record ActiveTenantNotFound;

    public partial record Invalid(ValidationErrors Errors);
}
