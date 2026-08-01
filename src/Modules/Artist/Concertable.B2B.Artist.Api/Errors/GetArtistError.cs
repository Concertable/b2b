using Concertable.Kernel.Errors;

namespace Concertable.B2B.Artist.Api.Errors;

internal sealed record GetArtistError : IError
{
    private GetArtistError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static GetArtistError NotFound(int artistId) =>
        new(ErrorDefinition.NotFound(
            "artist.get.not_found",
            $"Artist {artistId} was not found."));
}
