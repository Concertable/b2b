using Concertable.B2B.Artist.Application.Errors;
using Reunion.Errors;

namespace Concertable.B2B.Artist.UnitTests;

public sealed class ArtistErrorTests
{
    public static TheoryData<IError, string, string, ErrorKind> Cases => new()
    {
        {
            new CreateArtistError.ArtistAlreadyExists(),
            "artist.create.active_tenant_already_has_artist",
            "An artist profile already exists.",
            ErrorKind.Conflict
        },
        {
            new UpdateArtistError.ArtistNotFound(),
            "artist.update_not_found",
            "The artist profile does not exist.",
            ErrorKind.NotFound
        }
    };

    public static TheoryData<IError, string, string> ValidationCases => new()
    {
        {
            new CreateArtistError.Invalid(Errors),
            "create.artist_invalid",
            "The artist is invalid."
        },
        {
            new UpdateArtistError.Invalid(Errors),
            "update.artist_invalid",
            "The artist update is invalid."
        }
    };

    private static ValidationErrors Errors => new([new("Name", "Name is required.")]);

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

    [Theory]
    [MemberData(nameof(ValidationCases))]
    public void Definition_ValidationCase_ReturnsStableStructuredDefinition(
        IError error,
        string expectedCode,
        string expectedMessage)
    {
        var definition = Assert.IsType<ValidationError>(error.Definition);

        Assert.Equal(expectedCode, definition.Code);
        Assert.Equal(expectedMessage, definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Equal(["Name is required."], definition.Errors.Errors["Name"]);
    }
}
