using System.Net;
using Concertable.Testing.Integration.B2B;
using Concertable.B2B.User.Application.Requests;
using Concertable.B2B.User.Contracts;

namespace Concertable.B2B.User.IntegrationTests;

[Collection("Integration")]

public class UserApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public UserApiTests(ApiFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region UpdateLocation

    [Fact]
    public async Task UpdateLocation_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.PutAsync("/api/User/location", new UpdateLocationRequest(51.5, -0.1));

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateLocation_ShouldReturn200_WhenAuthenticated()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.PutAsync("/api/User/location", new UpdateLocationRequest(51.5, -0.1));

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadAsync<VenueManagerDto>();
        Assert.NotNull(user);
        Assert.Equal(fixture.SeedState.VenueManager1.Id, user.Id);
    }

    [Fact]
    public async Task UpdateLocation_ShouldPersistCoordinates()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        const double latitude = 53.4808;
        const double longitude = -2.2426;

        // Act
        var response = await client.PutAsync("/api/User/location", new UpdateLocationRequest(latitude, longitude));

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadAsync<VenueManagerDto>();
        Assert.NotNull(user);
        Assert.NotNull(user.Latitude);
        Assert.NotNull(user.Longitude);
    }

    #endregion
}
