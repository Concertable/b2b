using Concertable.B2B.Artist.Domain.Entities;
using Concertable.Contracts;
using Concertable.Contracts.Enums;
using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Artist.UnitTests;

public sealed class ArtistEntityTests
{
    [Fact]
    public void Create_InvalidProfile_ReturnsStructuredErrors()
    {
        var result = ArtistEntity.Create(
            Guid.NewGuid(),
            " ",
            new string('A', 1001),
            "banner",
            "avatar",
            new Point(1, 2),
            new Address("County", "Town"),
            "artist@example.com",
            [Genre.Rock]);

        Assert.True(result.TryGetError(out var errors));
        Assert.Equal(["Name is required."], errors.Errors["Name"]);
        Assert.Equal(["About must be 1000 characters or fewer."], errors.Errors["About"]);
    }

    [Fact]
    public void Update_InvalidProfile_ReturnsStructuredErrorsWithoutMutation()
    {
        var artist = Create();

        var result = artist.Update(
            new string('N', 101),
            "",
            "new-banner",
            [Genre.Jazz]);

        Assert.True(result.TryGetError(out var errors));
        Assert.Equal(["Name must be 100 characters or fewer."], errors.Errors["Name"]);
        Assert.Equal(["About is required."], errors.Errors["About"]);
        Assert.Equal("Artist", artist.Name);
        Assert.Equal("About", artist.About);
        Assert.Equal("banner", artist.BannerUrl);
        Assert.Equal([Genre.Rock], artist.Genres);
    }

    [Fact]
    public void SyncGenres_DuplicateGenre_IsStoredOnce()
    {
        var artist = Create();

        artist.SyncGenres([Genre.Jazz, Genre.Jazz, Genre.Rock]);

        Assert.Equal([Genre.Jazz, Genre.Rock], artist.Genres);
    }

    [Fact]
    public void Create_InvalidCollaboratorOutput_StillThrowsInvariantException()
    {
        Assert.Throws<DomainException>(() => ArtistEntity.Create(
            Guid.NewGuid(),
            "Artist",
            "About",
            "",
            "avatar",
            new Point(1, 2),
            new Address("County", "Town"),
            "artist@example.com",
            [Genre.Rock]));
    }

    private static ArtistEntity Create() => ArtistEntity.Create(
        Guid.NewGuid(),
        "Artist",
        "About",
        "banner",
        "avatar",
        new Point(1, 2),
        new Address("County", "Town"),
        "artist@example.com",
        [Genre.Rock])
        .Match(
            artist => artist,
            _ => throw new InvalidOperationException("Test artist is invalid."));
}
