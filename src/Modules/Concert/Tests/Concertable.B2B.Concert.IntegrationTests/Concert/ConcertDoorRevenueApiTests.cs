using System.Net;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Microsoft.AspNetCore.Mvc;
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
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking).Id;

        var before = await (await client.GetAsync($"/api/concert/application/{appId}")).Content.ReadAsync<MyDetailsResponse>();
        Assert.NotNull(before!.Actions!.DeclareDoorRevenue); // offered while ended, Booked, undeclared

        // Act
        var response = await client.PostAsync($"/api/concert/{concertId}/door-revenue", new { doorRevenue = DoorRevenue });

        // Assert — persisted; the action clears now the take is declared.
        await response.ShouldBe(HttpStatusCode.NoContent);
        var after = await (await client.GetAsync($"/api/concert/application/{appId}")).Content.ReadAsync<MyDetailsResponse>();
        Assert.Equal(DoorRevenue, after!.DoorRevenue);
        Assert.Null(after.Actions!.DeclareDoorRevenue);

        // ...and settlement now charges the artist's share of the declared take.
        await fixture.FinishConcertAsync(concertId);
        var payment = Assert.Single(fixture.SettlementClient.Payments);
        Assert.Equal(280m, payment.Amount);
    }

    [Fact]
    public async Task Declare_ShouldNotOfferLink_ForFixedFeeConcert()
    {
        // A fixed-fee (VenueHire) booking settles automatically — no door-take declaration.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.PaymentSimulator.SendWebhookAsync();

        var concert = await (await client.GetAsync($"/api/concert/application/{appId}")).Content.ReadAsync<MyDetailsResponse>();
        Assert.Null(concert!.Actions!.DeclareDoorRevenue);
    }

    [Fact]
    public async Task Declare_ShouldReturn403_WhenCallerIsArtist()
    {
        // Declaring the door take is a venue decision; the artist lacks the permission.
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking).Id;

        var response = await artistClient.PostAsync($"/api/concert/{concertId}/door-revenue", new { doorRevenue = DoorRevenue });

        await response.ShouldBe(HttpStatusCode.Forbidden);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concertId);
        Assert.Equal(ConcertState.Posted, persisted.State);
    }

    [Fact]
    public async Task Declare_ShouldReturn409_AfterConcertHasSettled()
    {
        // Arrange — declare, settle, complete.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking).Id;
        await client.PostAsync($"/api/concert/{concertId}/door-revenue", new { doorRevenue = DoorRevenue });
        await fixture.FinishConcertAsync(concertId);
        await fixture.PaymentSimulator.SendWebhookAsync();

        // Act — a second declaration once the booking is no longer Booked.
        var response = await client.PostAsync($"/api/concert/{concertId}/door-revenue", new { doorRevenue = 500m });

        // Assert — frozen after settlement.
        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Declare_ShouldReturnValidationProblem_WhenRevenueIsNegative()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking).Id;

        var response = await client.PostAsync(
            $"/api/concert/{concertId}/door-revenue",
            new { doorRevenue = -0.01m });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(["Door revenue must be zero or greater."], problem.Errors["DoorRevenue"]);
    }
}
