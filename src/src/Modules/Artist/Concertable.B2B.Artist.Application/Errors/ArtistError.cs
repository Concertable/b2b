using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ArtistError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var artistId) =>
            ErrorDefinition.NotFound<NotFound>($"Artist {artistId} was not found."),
        ActiveTenantNotFound =>
            ErrorDefinition.NotFound<ActiveTenantNotFound>(
                "No artist was found for the active tenant.")
    };

    [ErrorCode("artist.get.not_found")]
    public partial record NotFound(int ArtistId);

    [ErrorCode("artist.get.active_tenant_not_found")]
    public partial record ActiveTenantNotFound;
}
