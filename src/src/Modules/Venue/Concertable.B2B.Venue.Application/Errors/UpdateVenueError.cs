using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateVenueError : IError
{
    public ErrorDefinition Definition => this switch
    {
        VenueNotFound(var venueId) =>
            ErrorDefinition.NotFound<VenueNotFound>(
                $"Venue {venueId} was not found."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The venue update is invalid.",
                errors)
    };

    [ErrorCode("venue.update_not_found")]
    public partial record VenueNotFound(int VenueId);

    public partial record Invalid(ValidationErrors Errors);
}
