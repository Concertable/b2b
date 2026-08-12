using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateArtistError : IError
{
    public ErrorDefinition Definition => this switch
    {
        Forbidden =>
            ErrorDefinition.Forbidden<Forbidden>(
                "No active organization was found for the current user."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The artist is invalid.",
                errors)
    };

    [ErrorCode("artist.create_forbidden")]
    public partial record Forbidden;

    public partial record Invalid(ValidationErrors Errors);
}
