using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.User.Application.Requests;
using Concertable.B2B.User.Contracts;
using Concertable.B2B.User.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.User.IntegrationTests;

[Collection("Integration")]

public sealed class UserApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public UserApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

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
    public async Task UpdateLocation_ShouldReturnTyped401WithoutWritingLocation_WhenUserProjectionIsMissing()
    {
        var missingUser = UserEntity.FromRegistration(Guid.NewGuid(), "missing@test.com");
        var client = fixture.CreateClient(missingUser);

        var response = await client.PutAsync("/api/User/location", new UpdateLocationRequest(51.5, -0.1));

        await response.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Unauthorized, problem.Status);
        Assert.Equal("Unauthorized", problem.Title);
        Assert.Equal("The current user was not found.", problem.Detail);
        Assert.True(problem.Extensions.TryGetValue("code", out var code));
        Assert.Equal("user.location_unauthenticated", code?.ToString());

        using var scope = fixture.Services.CreateScope();
        var userModule = scope.ServiceProvider.GetRequiredService<IUserModule>();
        Assert.False((await userModule.GetByIdAsync(missingUser.Id)).TryGetValue(out _));
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
        var user = await response.Content.ReadAsync<UserDto>();
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
        var user = await response.Content.ReadAsync<UserDto>();
        Assert.NotNull(user);
        Assert.NotNull(user.Latitude);
        Assert.NotNull(user.Longitude);
    }

    #endregion
}
