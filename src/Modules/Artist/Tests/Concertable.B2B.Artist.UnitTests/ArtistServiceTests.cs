using Concertable.B2B.Artist.Application.Errors;
using Concertable.B2B.Artist.Application.DTOs;
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
    private readonly Mock<IArtistRepository> repository;
    private readonly Mock<IArtistReadRepository> readRepository;
    private readonly Mock<IImageService> imageService;
    private readonly Mock<ICurrentUser> currentUser;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly Mock<IGeocodingClient> geocodingClient;
    private readonly Mock<IGeometryProvider> geometryProvider;
    private readonly ArtistService service;

    public ArtistServiceTests()
    {
        this.repository = new Mock<IArtistRepository>();
        this.readRepository = new Mock<IArtistReadRepository>();
        this.imageService = new Mock<IImageService>();
        this.currentUser = new Mock<ICurrentUser>();
        this.tenantContext = new Mock<ITenantContext>();
        this.geocodingClient = new Mock<IGeocodingClient>();
        this.geometryProvider = new Mock<IGeometryProvider>();
        this.service = new ArtistService(
            this.repository.Object,
            this.readRepository.Object,
            this.imageService.Object,
            this.currentUser.Object,
            this.tenantContext.Object,
            this.geocodingClient.Object,
            this.geometryProvider.Object);
    }

    [Fact]
    public async Task GetDetailsAsync_ProfileMissing_ReturnsNone()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository
            .Setup(value => value.GetDetailsByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArtistDetails?)null);

        var result = await this.service.GetDetailsAsync();

        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task CreateAsync_InvalidProfile_MapsStructuredDomainFailure()
    {
        tenantContext.SetupGet(context => context.TenantId).Returns(Guid.NewGuid());
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
            Name = string.Empty,
            About = string.Empty,
            Latitude = 1,
            Longitude = 2,
            Banner = Mock.Of<IFormFile>(),
            Avatar = Mock.Of<IFormFile>()
        };

        var result = await this.service.CreateAsync(request);

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
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        var artist = ArtistEntity.Create(
            tenantId,
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
            .Setup(value => value.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        var request = new UpdateArtistRequest
        {
            Name = string.Empty,
            About = string.Empty,
            Latitude = 1,
            Longitude = 2,
            Banner = Mock.Of<IFormFile>()
        };

        var result = await this.service.UpdateAsync(request);

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

}
