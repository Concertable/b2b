using Concertable.B2B.Artist.Application.Errors;
using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Artist.Application.Requests;
using Concertable.B2B.Artist.Domain.Entities;
using Concertable.B2B.Artist.Infrastructure.Services;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Shared.Geocoding.Application;
using Concertable.Shared.Imaging.Application;
using Microsoft.AspNetCore.Http;
using Moq;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Artist.UnitTests;

public sealed class ArtistServiceTests
{
    private readonly Mock<IArtistRepository> repository = new();
    private readonly Mock<IPublicArtistRepository> publicRepository = new();
    private readonly Mock<IImageService> imageService = new();
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly Mock<IGeocodingClient> geocodingClient = new();
    private readonly Mock<IGeometryProvider> geometryProvider = new();

    [Fact]
    public async Task CreateAsync_InvalidProfile_MapsStructuredDomainFailure()
    {
        tenantContext.SetupGet(context => context.HasTenant).Returns(true);
        currentUser.SetupGet(user => user.Id).Returns(Guid.NewGuid());
        currentUser.SetupGet(user => user.Email).Returns("artist@example.com");
        imageService
            .SetupSequence(service => service.UploadAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("banner")
            .ReturnsAsync("avatar");
        geocodingClient
            .Setup(client => client.GetLocationAsync(1, 2))
            .ReturnsAsync(new Address("County", "Town"));
        geometryProvider
            .Setup(provider => provider.CreatePoint(1, 2))
            .Returns(new Point(1, 2));
        var request = new CreateArtistRequest
        {
            Name = "",
            About = "",
            Latitude = 1,
            Longitude = 2,
            Banner = Mock.Of<IFormFile>(),
            Avatar = Mock.Of<IFormFile>()
        };

        var result = await CreateService().CreateAsync(request);

        Assert.True(result.TryGetError(out var error));
        var invalid = Assert.IsType<CreateArtistError.Invalid>(error);
        Assert.Equal(["Name is required."], invalid.Errors.Errors["Name"]);
        Assert.Equal(["About is required."], invalid.Errors.Errors["About"]);
        imageService.Verify(
            service => service.UploadAsync(It.IsAny<IFormFile>()),
            Times.Never);
        repository.Verify(
            value => value.AddAsync(It.IsAny<ArtistEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidProfile_MapsFailureBeforeDownstreamUpdates()
    {
        var artist = ArtistEntity.Create(
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
                value => value,
                _ => throw new InvalidOperationException("Test artist is invalid."));
        repository
            .Setup(value => value.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        var request = new UpdateArtistRequest
        {
            Name = "",
            About = "",
            Latitude = 1,
            Longitude = 2,
            Banner = Mock.Of<IFormFile>()
        };

        var result = await CreateService().UpdateAsync(42, request);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<UpdateArtistError.Invalid>(error);
        geocodingClient.Verify(
            client => client.GetLocationAsync(It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);
        imageService.Verify(
            service => service.ReplaceAsync(It.IsAny<IFormFile>(), It.IsAny<string?>()),
            Times.Never);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private ArtistService CreateService() => new(
        repository.Object,
        publicRepository.Object,
        imageService.Object,
        currentUser.Object,
        tenantContext.Object,
        geocodingClient.Object,
        geometryProvider.Object);
}
