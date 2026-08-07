using Concertable.B2B.Artist.Application.Errors;
using Concertable.Kernel.Errors;

namespace Concertable.B2B.Artist.UnitTests;

public sealed class ArtistErrorTests
{
    public static TheoryData<IError, string, string, ErrorKind> Cases => new()
    {
        {
            new ArtistError.NotFound(42),
            "artist.get.not_found",
            "Artist 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new ArtistError.CurrentTenantNotFound(),
            "artist.get.current_tenant_not_found",
            "No artist was found for the current tenant.",
            ErrorKind.NotFound
        },
        {
            new CreateArtistError.Forbidden(),
            "artist.create_forbidden",
            "No active organization was found for the current user.",
            ErrorKind.Forbidden
        },
        {
            new UpdateArtistError.NotFound(42),
            "artist.update_not_found",
            "Artist 42 was not found.",
            ErrorKind.NotFound
        }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Definition_ErrorCase_ReturnsStableDefinition(
        IError error,
        string expectedCode,
        string expectedMessage,
        ErrorKind expectedKind)
    {
        var definition = error.Definition;

        Assert.Equal(expectedCode, definition.Code);
        Assert.Equal(expectedMessage, definition.Message);
        Assert.Equal(expectedKind, definition.Kind);
    }
}
