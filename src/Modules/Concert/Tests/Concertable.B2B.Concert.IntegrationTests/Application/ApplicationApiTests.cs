using System.Net;
using System.Text.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]

public sealed class ApplicationApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ApplicationApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task GetCurrentForVenue_ShouldReturnApplicationList()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/Application/venue/current");

        await response.ShouldBe(HttpStatusCode.OK);
        var applications = await response.Content.ReadAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, applications.ValueKind);
    }

    [Fact]
    public async Task GetCurrentForArtist_ShouldReturnApplicationList()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/Application/artist/current");

        await response.ShouldBe(HttpStatusCode.OK);
        var applications = await response.Content.ReadAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, applications.ValueKind);
    }

    #region Accept

    [Fact]
    public async Task Accept_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WhenCalledByDifferentVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Apply

    [Fact]
    public async Task Apply_ShouldReturn400_WhenSameArtistReappliesAfterWithdraw()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;
        var withdraw = await client.PostAsync($"/api/Application/{appId}/withdraw", (object?)null);
        await withdraw.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var response = await client.PostAsync($"/api/Application/{opportunityId}", new { eSignature = new { signatoryName = "Aretha Artist" } });

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion
}
