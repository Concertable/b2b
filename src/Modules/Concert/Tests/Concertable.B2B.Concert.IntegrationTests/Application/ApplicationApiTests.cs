using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]

public sealed class ApplicationApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ApplicationApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region Accept

    [Fact]
    public async Task Accept_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/accept", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WhenCalledByDifferentVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/accept", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion
}
