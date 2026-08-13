using System.Net;
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

    #region Eligibility

    [Fact]
    public async Task CanApply_EligibleArtist_ReturnsTrue()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync(
            $"/api/Application/opportunity/{fixture.SeedState.FreshVenueHireOpportunity.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.True(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task CanApply_MissingArtist_ReturnsFalse()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync(
            $"/api/Application/opportunity/{fixture.SeedState.FreshVenueHireOpportunity.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.False(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task CanAccept_EligibleApplication_ReturnsTrue()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync(
            $"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.True(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task CanAccept_MissingApplication_ReturnsFalse()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/Application/2147483647/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.False(await response.Content.ReadAsync<bool>());
    }

    #endregion

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
    public async Task Accept_ShouldReturn404_WhenCalledByDifferentVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
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
