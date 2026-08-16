using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateArtistError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var artistId) =>
            ErrorDefinition.NotFound<NotFound>($"Artist {artistId} was not found."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The artist update is invalid.",
                errors)
    };

    [ErrorCode("artist.update_not_found")]
    public partial record NotFound(int ArtistId);

    public partial record Invalid(ValidationErrors Errors);
}
