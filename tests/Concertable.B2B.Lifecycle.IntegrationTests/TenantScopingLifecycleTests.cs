using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Payment.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Lifecycle.IntegrationTests;

[Collection("Integration")]
public sealed class TenantScopingLifecycleTests : IAsyncLifetime
{
    private readonly LifecycleApiFixture fixture;

    public TenantScopingLifecycleTests(LifecycleApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Accept_PropagatesTheTenantSnapshotThroughBookingAndConcertBoundaries()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await AcceptAndConfirmAsync(venueClient, applicationId);

        var command = fixture.PaymentTransport.SingleCommand<CaptureEscrowCommand>();
        Assert.Equal(TenantOf(fixture.SeedState.VenueManager1.Id), command.PayerId);
        Assert.Equal(TenantOf(fixture.SeedState.ArtistManager1.Id), command.PayeeId);
        var concert = await GetConcertAsync(venueClient, applicationId);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        await (await artistClient.GetAsync($"/api/organization/concert/{concert.Id}"))
            .ShouldBe(HttpStatusCode.OK);
        var stranger = fixture.CreateClient(fixture.SeedState.VenueManager2);
        await (await stranger.GetAsync($"/api/organization/concert/{concert.Id}"))
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OrganizationConcertRead_ScopesActionsToPartiesAndKeepsPublicReadActionFree()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await AcceptAndConfirmAsync(venueClient, applicationId);
        var concert = await GetConcertAsync(venueClient, applicationId);

        var venueRead = await venueClient.GetAsync($"/api/organization/concert/{concert.Id}");
        await venueRead.ShouldBe(HttpStatusCode.OK);
        var venueConcert = await venueRead.Content.ReadAsync<OrganizationConcertBoundaryResponse>();
        Assert.NotNull(venueConcert);
        Assert.Equal($"/api/concert/{concert.Id}/contract/pdf", venueConcert.Actions.Contract?.Href);
        Assert.NotNull(venueConcert.Actions.Cancel);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var artistRead = await artistClient.GetAsync($"/api/organization/concert/{concert.Id}");
        await artistRead.ShouldBe(HttpStatusCode.OK);
        var artistConcert = await artistRead.Content.ReadAsync<OrganizationConcertBoundaryResponse>();
        Assert.NotNull(artistConcert?.Actions.Contract);
        var stranger = fixture.CreateClient(fixture.SeedState.VenueManager2);
        await (await stranger.GetAsync($"/api/organization/concert/{concert.Id}"))
            .ShouldBe(HttpStatusCode.NotFound);
        var publicRead = await stranger.GetAsync($"/api/concert/{concert.Id}");
        await publicRead.ShouldBe(HttpStatusCode.OK);
        var publicConcert = await publicRead.Content.ReadAsync<PublicConcertBoundaryResponse>();
        Assert.Equal(concert.Id, publicConcert?.Id);
    }

    private async Task AcceptAndConfirmAsync(HttpClient client, int applicationId)
    {
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.PaymentSimulator.SendWebhookAsync();
    }

    private Guid TenantOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(value => value.CreatedByUserId == userId).Id;

    private static async Task<ConcertBoundaryResponse> GetConcertAsync(
        HttpClient client,
        int applicationId)
    {
        var response = await client.GetAsync($"/api/concert/application/{applicationId}");
        await response.ShouldBe(HttpStatusCode.OK);
        var concert = await response.Content.ReadAsync<ConcertBoundaryResponse>();
        Assert.NotNull(concert);
        return concert;
    }

    private sealed record ConcertBoundaryResponse(int Id);
    private sealed record OrganizationConcertBoundaryResponse(ConcertActionsBoundaryResponse Actions);
    private sealed record ConcertActionsBoundaryResponse(
        ActionBoundaryResponse? Cancel,
        ActionBoundaryResponse? Contract);
    private sealed record ActionBoundaryResponse(string Href);
    private sealed record PublicConcertBoundaryResponse(int Id);
}
