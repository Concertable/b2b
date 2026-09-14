using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateArtistError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ArtistAlreadyExists =>
            ErrorDefinition.Conflict<ArtistAlreadyExists>(
                "An artist profile already exists."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The artist is invalid.",
                errors)
    };

    [ErrorCode("artist.create.active_tenant_already_has_artist")]
    public partial record ArtistAlreadyExists;

    public partial record Invalid(ValidationErrors Errors);
}
