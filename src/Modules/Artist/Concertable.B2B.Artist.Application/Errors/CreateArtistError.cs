using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union]
internal partial record CreateArtistError : IError
{
    partial record NoActiveTenant;

    public static CreateArtistError Forbidden() => new NoActiveTenant();

    public ErrorDefinition Definition => ErrorDefinition.Forbidden(
        "artist.create_forbidden",
        "No active organization was found for the current user.");
}
