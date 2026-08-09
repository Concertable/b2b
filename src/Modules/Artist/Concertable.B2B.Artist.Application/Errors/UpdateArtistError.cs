using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateArtistError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var artistId) =>
            ErrorDefinition.For<UpdateArtistError>().NotFound<NotFound>($"Artist {artistId} was not found.")
    };

    [ErrorCode("artist.update_not_found")]
    public partial record NotFound(int ArtistId);
}
