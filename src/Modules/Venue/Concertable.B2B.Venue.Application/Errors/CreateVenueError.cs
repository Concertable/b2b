using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateVenueError : IError
{
    public ErrorDefinition Definition => this switch
    {
        VenueAlreadyExists =>
            ErrorDefinition.Conflict<VenueAlreadyExists>(
                "A venue profile already exists."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The venue is invalid.",
                errors)
    };

    [ErrorCode("venue.create.active_tenant_already_has_venue")]
    public partial record VenueAlreadyExists;

    public partial record Invalid(ValidationErrors Errors);
}
