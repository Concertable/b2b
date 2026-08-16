using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateArtistError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ActiveTenantNotFound =>
            ErrorDefinition.NotFound<ActiveTenantNotFound>(
                "No artist was found for the active tenant."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The artist update is invalid.",
                errors)
    };

    [ErrorCode("artist.update_not_found")]
    public partial record ActiveTenantNotFound;

    public partial record Invalid(ValidationErrors Errors);
}
