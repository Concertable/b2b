using System.Net;
using Concertable.Testing.Integration.B2B;
using Xunit;

namespace Concertable.Concert.IntegrationTests.Application;

[Collection("Integration")]

public class ApplicationApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ApplicationApiTests(ApiFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region Accept

    [Fact]
    public async Task Accept_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/accept", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WhenCalledByDifferentVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager2);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/accept", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion
}
