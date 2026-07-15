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
public sealed class ConcertDoorRevenueApiTests : IAsyncLifetime
{
    private const decimal DoorRevenue = 200m;

    private readonly ConcertApiFixture fixture;

    public ConcertDoorRevenueApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Declare_ShouldOfferLinkThenPersistAndSettle_WhenVenueDeclaresOverHttp()
    {
        // Arrange — a past, still-Booked DoorSplit gig awaiting its door take.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.PastDoorSplitApp.Id;
        var concertId = fixture.SeedState.PastDoorSplitBooking.Concert!.Id;
        var deal = fixture.SeedState.PastDoorSplitAppDeal;

        var before = await (await client.GetAsync($"/api/Concert/application/{appId}")).Content.ReadAsync<ConcertDetailsResponse>();
        Assert.NotNull(before!.Actions!.DeclareDoorRevenue); // offered while ended, Booked, undeclared

        // Act
        var response = await client.PostAsync($"/api/Concert/{concertId}/door-revenue", new { doorRevenue = DoorRevenue });

        // Assert — persisted; the action clears now the take is declared.
        await response.ShouldBe(HttpStatusCode.NoContent);
        var after = await (await client.GetAsync($"/api/Concert/application/{appId}")).Content.ReadAsync<ConcertDetailsResponse>();
        Assert.Equal(DoorRevenue, after!.DoorRevenue);
        Assert.Null(after.Actions!.DeclareDoorRevenue);

        // ...and settlement now charges the artist's share of the declared take.
        await fixture.FinishConcertAsync(concertId);
        var payment = Assert.Single(fixture.ManagerPaymentClient.Payments);
        var concert = fixture.SeedState.PastDoorSplitBooking.Concert!;
        Assert.Equal(deal.CalculateArtistShare(concert.TicketsSold * concert.Price + DoorRevenue), payment.Amount);
    }

    [Fact]
    public async Task Declare_ShouldNotOfferLink_ForFixedFeeConcert()
    {
        // A fixed-fee (VenueHire) booking settles automatically — no door-take declaration.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        await client.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.StripeClient.SendWebhookAsync();

        var concert = await (await client.GetAsync($"/api/Concert/application/{appId}")).Content.ReadAsync<ConcertDetailsResponse>();
        Assert.Null(concert!.Actions!.DeclareDoorRevenue);
    }

    [Fact]
    public async Task Declare_ShouldReturn403_WhenCallerIsArtist()
    {
        // Declaring the door take is a venue decision; the artist lacks the permission.
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var concertId = fixture.SeedState.PastDoorSplitBooking.Concert!.Id;

        var response = await artistClient.PostAsync($"/api/Concert/{concertId}/door-revenue", new { doorRevenue = DoorRevenue });

        await response.ShouldBe(HttpStatusCode.Forbidden);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == fixture.SeedState.PastDoorSplitApp.Id);
        Assert.Equal(LifecycleState.Booked, application.State);
    }

    [Fact]
    public async Task Declare_ShouldReturn409_AfterConcertHasSettled()
    {
        // Arrange — declare, settle, complete.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var concertId = fixture.SeedState.PastDoorSplitBooking.Concert!.Id;
        await client.PostAsync($"/api/Concert/{concertId}/door-revenue", new { doorRevenue = DoorRevenue });
        await fixture.FinishConcertAsync(concertId);
        await fixture.StripeClient.SendWebhookAsync();

        // Act — a second declaration once the booking is no longer Booked.
        var response = await client.PostAsync($"/api/Concert/{concertId}/door-revenue", new { doorRevenue = 500m });

        // Assert — frozen after settlement.
        await response.ShouldBe(HttpStatusCode.Conflict);
    }
}
