using System.Net;
using Concertable.User.Application.Requests;
using Concertable.User.Contracts;

namespace Concertable.User.IntegrationTests;

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
        var client = fixture.CreateClient(fixture.SeedData.Customer);

        // Act
        var response = await client.PutAsync("/api/User/location", new UpdateLocationRequest(51.5, -0.1));

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadAsync<CustomerDto>();
        Assert.NotNull(user);
        Assert.Equal(fixture.SeedData.Customer.Id, user.Id);
    }

    [Fact]
    public async Task UpdateLocation_ShouldPersistCoordinates()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.Customer);
        const double latitude = 53.4808;
        const double longitude = -2.2426;

        // Act
        var response = await client.PutAsync("/api/User/location", new UpdateLocationRequest(latitude, longitude));

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadAsync<CustomerDto>();
        Assert.NotNull(user);
        Assert.NotNull(user.Latitude);
        Assert.NotNull(user.Longitude);
    }

    #endregion
}
