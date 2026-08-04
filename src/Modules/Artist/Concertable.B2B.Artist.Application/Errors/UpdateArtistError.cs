namespace Concertable.B2B.Artist.Application.Errors;

internal sealed record UpdateArtistError(ErrorDefinition Definition) : IError
{
    internal static UpdateArtistError NotFound(int artistId) =>
        new(ErrorDefinition.NotFound(
            "artist.update_not_found",
            $"Artist {artistId} was not found."));
}
