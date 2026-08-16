using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateVenueError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant =>
            ErrorDefinition.Forbidden<NoActiveTenant>(
                "No active organization was found for the current user."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The venue is invalid.",
                errors)
    };

    [ErrorCode("venue.create_forbidden")]
    public partial record NoActiveTenant;

    public partial record Invalid(ValidationErrors Errors);
}
