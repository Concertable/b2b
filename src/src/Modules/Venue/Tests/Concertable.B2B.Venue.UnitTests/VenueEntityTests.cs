using Concertable.B2B.Venue.Domain.Entities;
using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Venue.UnitTests;

public sealed class VenueEntityTests
{
    [Fact]
    public void Create_InvalidProfile_ReturnsStructuredErrors()
    {
        var result = VenueEntity.Create(
            Guid.NewGuid(),
            " ",
            new string('A', 1001),
            "banner",
            "avatar",
            new Point(1, 2),
            new Address("County", "Town"),
            "venue@example.com");

        Assert.True(result.TryGetError(out var errors));
        Assert.Equal(["Name is required."], errors.Errors["Name"]);
        Assert.Equal(["About must be 1000 characters or fewer."], errors.Errors["About"]);
    }

    [Fact]
    public void Update_InvalidProfile_ReturnsStructuredErrorsWithoutMutation()
    {
        var venue = Create();

        var result = venue.Update(new string('N', 101), "", "new-banner");

        Assert.True(result.TryGetError(out var errors));
        Assert.Equal(["Name must be 100 characters or fewer."], errors.Errors["Name"]);
        Assert.Equal(["About is required."], errors.Errors["About"]);
        Assert.Equal("Venue", venue.Name);
        Assert.Equal("About", venue.About);
        Assert.Equal("banner", venue.BannerUrl);
    }

    [Fact]
    public void Create_InvalidCollaboratorOutput_StillThrowsInvariantException()
    {
        Assert.Throws<DomainException>(() => VenueEntity.Create(
            Guid.NewGuid(),
            "Venue",
            "About",
            "banner",
            "",
            new Point(1, 2),
            new Address("County", "Town"),
            "venue@example.com"));
    }

    private static VenueEntity Create() => VenueEntity.Create(
        Guid.NewGuid(),
        "Venue",
        "About",
        "banner",
        "avatar",
        new Point(1, 2),
        new Address("County", "Town"),
        "venue@example.com")
        .Match(
            venue => venue,
            _ => throw new InvalidOperationException("Test venue is invalid."));
}
