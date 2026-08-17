using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateArtistError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ActiveTenantAlreadyHasArtist =>
            ErrorDefinition.Conflict<ActiveTenantAlreadyHasArtist>(
                "The active organization already has an artist."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The artist is invalid.",
                errors)
    };

    [ErrorCode("artist.create.active_tenant_already_has_artist")]
    public partial record ActiveTenantAlreadyHasArtist;

    public partial record Invalid(ValidationErrors Errors);
}
