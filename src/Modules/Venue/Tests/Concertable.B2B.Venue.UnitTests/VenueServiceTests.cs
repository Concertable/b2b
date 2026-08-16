using Concertable.B2B.Venue.Application.Errors;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Application.Requests;
using Concertable.B2B.Venue.Domain.Entities;
using Concertable.B2B.Venue.Infrastructure.Services;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Shared.Geocoding.Application;
using Concertable.Shared.Imaging.Application;
using Microsoft.AspNetCore.Http;
using Moq;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Venue.UnitTests;

public sealed class VenueServiceTests
{
    private readonly Mock<IVenueRepository> repository = new();
    private readonly Mock<IVenueReadRepository> readRepository = new();
    private readonly Mock<IVenueAdminRepository> adminRepository = new();
    private readonly Mock<IImageService> imageService = new();
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly Mock<IGeocodingClient> geocodingClient = new();
    private readonly Mock<IGeometryProvider> geometryProvider = new();

    [Fact]
    public async Task CreateAsync_InvalidProfile_MapsStructuredDomainFailure()
    {
        tenantContext.SetupGet(context => context.TenantId).Returns(Guid.NewGuid());
        currentUser.SetupGet(user => user.Id).Returns(Guid.NewGuid());
        currentUser.SetupGet(user => user.Email).Returns("venue@example.com");
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
        var request = new CreateVenueRequest
        {
            Name = string.Empty,
            About = string.Empty,
            Latitude = 1,
            Longitude = 2,
            Banner = Mock.Of<IFormFile>(),
            Avatar = Mock.Of<IFormFile>()
        };

        var result = await CreateService().CreateForActiveTenantAsync(request);

        Assert.True(result.TryGetError(out var error));
        var invalid = Assert.IsType<CreateVenueError.Invalid>(error);
        Assert.Equal(["Name is required."], invalid.Errors.Errors["Name"]);
        Assert.Equal(["About is required."], invalid.Errors.Errors["About"]);
        imageService.Verify(
            service => service.UploadAsync(It.IsAny<IFormFile>()),
            Times.Never);
        repository.Verify(
            value => value.AddAsync(It.IsAny<VenueEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidProfile_MapsFailureBeforeDownstreamUpdates()
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        var venue = VenueEntity.Create(
            tenantId,
            "Venue",
            "About",
            "banner",
            "avatar",
            new Point(1, 2),
            new Address("County", "Town"),
            "venue@example.com")
            .Match(
                value => value,
                _ => throw new InvalidOperationException("Test venue is invalid."));
        repository
            .Setup(value => value.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venue);
        var request = new UpdateVenueRequest
        {
            Name = string.Empty,
            About = string.Empty,
            Latitude = 1,
            Longitude = 2,
            Banner = Mock.Of<IFormFile>()
        };

        var result = await CreateService().UpdateForActiveTenantAsync(request);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<UpdateVenueError.Invalid>(error);
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

    private VenueService CreateService() => new(
        repository.Object,
        readRepository.Object,
        adminRepository.Object,
        imageService.Object,
        currentUser.Object,
        tenantContext.Object,
        geocodingClient.Object,
        geometryProvider.Object);
}
