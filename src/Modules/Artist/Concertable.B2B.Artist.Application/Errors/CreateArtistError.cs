namespace Concertable.B2B.Artist.Application.Errors;

internal sealed record CreateArtistError(ErrorDefinition Definition) : IError
{
    internal static readonly CreateArtistError Forbidden = new(
        ErrorDefinition.Forbidden(
            "artist.create_forbidden",
            "No active organization was found for the current user."));
}
