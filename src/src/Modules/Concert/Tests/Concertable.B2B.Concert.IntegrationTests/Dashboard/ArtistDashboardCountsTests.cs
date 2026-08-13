using System.Net;
using Concertable.B2B.Concert.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Dashboard;

[Collection("Integration")]
public sealed class ArtistDashboardCountsTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ArtistDashboardCountsTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task GetArtistDashboardCounts_CountsAcceptedCheckoutCapableApplication_AfterVenueAccepts()
    {
        var artistId = fixture.SeedState.FlatFeeApp.ArtistId;
        var before = await GetAcceptedAwaitingCheckoutAsync(artistId);

        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/checkout");
        var acceptResponse = await venueClient.PostAsync(
            $"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetAcceptedAwaitingCheckoutAsync(artistId);

        Assert.Equal(before + 1, after);
    }

    private async Task<int> GetAcceptedAwaitingCheckoutAsync(int artistId)
    {
        using var scope = fixture.Services.CreateScope();
        var concertModule = scope.ServiceProvider.GetRequiredService<IConcertModule>();
        var counts = await concertModule.GetArtistDashboardCountsAsync(artistId);
        Assert.NotNull(counts);
        return counts!.AcceptedAwaitingCheckout;
    }
}
