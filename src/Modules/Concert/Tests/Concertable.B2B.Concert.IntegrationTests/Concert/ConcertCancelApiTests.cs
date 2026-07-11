using System.Net;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertCancelApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ConcertCancelApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Cancel_ShouldRefundEscrowAndMarkCancelled_ForFlatFee()
    {
        // Arrange — drive the FlatFee booking to Booked (escrow held).
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/Application/{appId}/checkout");
        var acceptResponse = await client.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        var concertResponse = await client.GetAsync($"/api/Concert/application/{appId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertDetailsResponse>();
        Assert.NotNull(concert!.Actions!.Cancel); // cancel offered while Booked

        var booking = await fixture.ConcertReads.Set<BookingEntity>().FirstAsync(b => b.ApplicationId == appId);

        // Act
        var cancelResponse = await client.PostAsync($"/api/Concert/{concert.Id}/cancel");

        // Assert — booking dead, escrow refunded, cancel no longer offered.
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        Assert.Contains(booking.Id, fixture.EscrowClient.Refunds);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == appId);
        Assert.Equal(LifecycleState.Cancelled, application.State);

        var afterResponse = await client.GetAsync($"/api/Concert/application/{appId}");
        var after = await afterResponse.Content.ReadAsync<ConcertDetailsResponse>();
        Assert.Null(after!.Actions!.Cancel);
    }

    [Fact]
    public async Task Cancel_ShouldRefundEscrowAndMarkCancelled_ForVenueHire()
    {
        // Arrange — VenueHire is prepaid; accept + webhook reaches Booked with escrow held.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        await client.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.StripeClient.SendWebhookAsync();

        var concertResponse = await client.GetAsync($"/api/Concert/application/{appId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertDetailsResponse>();
        var booking = await fixture.ConcertReads.Set<BookingEntity>().FirstAsync(b => b.ApplicationId == appId);

        // Act
        var cancelResponse = await client.PostAsync($"/api/Concert/{concert!.Id}/cancel");

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        Assert.Contains(booking.Id, fixture.EscrowClient.Refunds);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == appId);
        Assert.Equal(LifecycleState.Cancelled, application.State);
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_ForDoorSplit_WhereNoEscrowIsHeld()
    {
        // Arrange — DoorSplit holds no escrow at Booked (verify is a SetupIntent); refund is a no-op.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.DoorSplitApp.Id;
        await client.PostAsync($"/api/Application/{appId}/checkout");
        var acceptResponse = await client.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" }, paymentMethodId = "pm_card_visa" });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        var concertResponse = await client.GetAsync($"/api/Concert/application/{appId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertDetailsResponse>();

        // Act
        var cancelResponse = await client.PostAsync($"/api/Concert/{concert!.Id}/cancel");

        // Assert — cancels cleanly; no escrow hold existed, so the refund is a correct no-op.
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        Assert.Empty(fixture.EscrowClient.Holds);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == appId);
        Assert.Equal(LifecycleState.Cancelled, application.State);
    }

    [Fact]
    public async Task Cancel_ShouldReturn403_WhenCallerIsArtist()
    {
        // Arrange — reach Booked as the venue, then have the artist attempt the cancel.
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await venueClient.PostAsync($"/api/Application/{appId}/checkout");
        await venueClient.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.StripeClient.SendWebhookAsync();
        var concert = await (await venueClient.GetAsync($"/api/Concert/application/{appId}")).Content.ReadAsync<ConcertDetailsResponse>();

        // Act
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await artistClient.PostAsync($"/api/Concert/{concert!.Id}/cancel");

        // Assert — cancelling is a venue decision; the artist lacks the permission.
        await response.ShouldBe(HttpStatusCode.Forbidden);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == appId);
        Assert.Equal(LifecycleState.Booked, application.State);
    }
}
