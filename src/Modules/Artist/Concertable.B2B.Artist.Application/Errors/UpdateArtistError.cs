using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union]
internal partial record UpdateArtistError : IError
{
    partial record ArtistNotFound(int ArtistId);

    public static UpdateArtistError NotFound(int artistId) => new ArtistNotFound(artistId);

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        error => ErrorDefinition.NotFound(
            "artist.update_not_found",
            $"Artist {error.ArtistId} was not found."));
}
