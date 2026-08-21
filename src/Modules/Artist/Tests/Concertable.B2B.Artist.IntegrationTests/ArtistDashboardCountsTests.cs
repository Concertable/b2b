using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit.Abstractions;

namespace Concertable.B2B.Artist.IntegrationTests;

[Collection("Integration")]
public sealed class ArtistDashboardCountsTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ArtistDashboardCountsTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task GetArtistDashboardCounts_CountsAcceptedCheckoutCapableApplication_AfterVenueAccepts()
    {
        var before = await GetAcceptedAwaitingCheckoutAsync();

        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");
        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetAcceptedAwaitingCheckoutAsync();

        Assert.Equal(before + 1, after);
    }

    private async Task<int> GetAcceptedAwaitingCheckoutAsync()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await client.GetAsync("/api/artist-dashboard/kpis");
        await response.ShouldBe(HttpStatusCode.OK);
        var counts = await response.Content.ReadAsync<ArtistDashboardBoundaryResponse>();
        Assert.NotNull(counts);
        return counts.AcceptedAwaitingCheckout;
    }

    private sealed record ArtistDashboardBoundaryResponse(int AcceptedAwaitingCheckout);
}
