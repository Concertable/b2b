using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ArtistError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var artistId) =>
            ErrorDefinition.For<ArtistError>().NotFound<NotFound>($"Artist {artistId} was not found."),
        CurrentTenantNotFound =>
            ErrorDefinition.For<ArtistError>().NotFound<CurrentTenantNotFound>(
                "No artist was found for the current tenant.")
    };

    [ErrorCode("artist.get.not_found")]
    public partial record NotFound(int ArtistId);

    [ErrorCode("artist.get.current_tenant_not_found")]
    public partial record CurrentTenantNotFound;
}
