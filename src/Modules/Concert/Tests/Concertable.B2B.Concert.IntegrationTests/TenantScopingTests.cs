using System.Net;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using static Concertable.B2B.Concert.IntegrationTests.Opportunity.OpportunityRequestBuilders;

namespace Concertable.B2B.Concert.IntegrationTests;

[Collection("Integration")]
public sealed class TenantScopingTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public TenantScopingTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Guid TenantOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    /// <summary>
    /// Applying snapshots both parties onto the application: the venue side from the opportunity's
    /// tenant, the artist side from the applier's own tenant. Everything downstream inherits this pair.
    /// </summary>
    [Fact]
    public async Task Apply_StampsBothPartyTenantsOnTheApplication()
    {
        // Arrange — venue manager creates a fresh FlatFee opportunity
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var oppResponse = await venueClient.PostAsync("/api/Opportunity",
            BuildRequest(new FlatFeeContract { PaymentMethod = PaymentMethod.Cash, Fee = 500 }));
        var opportunity = await oppResponse.Content.ReadAsync<OpportunityResponse>();

        // Act — artist applies
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var applyResponse = await artistClient.PostAsync($"/api/Application/{opportunity!.Id}", new { eSignature = new { signatoryName = "Test Signatory" } });
        await applyResponse.ShouldBe(HttpStatusCode.Created);

        // Assert — the row carries the frozen pair
        var application = await fixture.ConcertReads.Set<ApplicationEntity>()
            .FirstAsync(a => a.OpportunityId == opportunity.Id);
        Assert.Equal(TenantOf(fixture.SeedState.VenueManager1.Id), application.VenueTenantId);
        Assert.Equal(TenantOf(fixture.SeedState.ArtistManager1.Id), application.ArtistTenantId);
    }

    /// <summary>
    /// Accepting inherits the application's pair onto the booking, and the draft concert inherits it
    /// from the booking — the frozen-at-accept snapshot settlement later pays from.
    /// </summary>
    [Fact]
    public async Task Accept_InheritsTheTenantSnapshotOntoBookingAndConcert()
    {
        // Arrange + Act — full FlatFee accept flow
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/checkout");
        var acceptResponse = await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert — application, booking and concert all carry the same pair
        var application = await fixture.ConcertReads.Set<ApplicationEntity>()
            .FirstAsync(a => a.Id == fixture.SeedState.FlatFeeApp.Id);
        var booking = await fixture.ConcertReads.Set<BookingEntity>()
            .FirstAsync(b => b.ApplicationId == fixture.SeedState.FlatFeeApp.Id);
        var concert = await fixture.ConcertReads.Set<ConcertEntity>()
            .FirstAsync(c => c.BookingId == booking.Id);

        Assert.Equal(TenantOf(fixture.SeedState.VenueManager1.Id), application.VenueTenantId);
        Assert.Equal(TenantOf(fixture.SeedState.ArtistManager1.Id), application.ArtistTenantId);
        Assert.Equal((application.VenueTenantId, application.ArtistTenantId), (booking.VenueTenantId, booking.ArtistTenantId));
        Assert.Equal((application.VenueTenantId, application.ArtistTenantId), (concert.VenueTenantId, concert.ArtistTenantId));
    }

    /// <summary>
    /// The two-party "Tenant" filter: an application is visible to its venue side and its artist side,
    /// and does not exist for anyone else — the filter answers 404, not 403, so third parties can't
    /// even probe which ids exist.
    /// </summary>
    [Fact]
    public async Task Application_IsVisibleToBothPartiesAndInvisibleToThirdPartyTenants()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;

        var venueParty = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await (await venueParty.GetAsync($"/api/Application/{applicationId}")).ShouldBe(HttpStatusCode.OK);

        var artistParty = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        await (await artistParty.GetAsync($"/api/Application/{applicationId}")).ShouldBe(HttpStatusCode.OK);

        var thirdParty = fixture.CreateClient(fixture.SeedState.VenueManager2);
        await (await thirdParty.GetAsync($"/api/Application/{applicationId}")).ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Concert stays deliberately UNFILTERED (Bucket C): Search finds it, B2B serves its public details
    /// page — a manager from an unrelated tenant can still read a posted concert.
    /// </summary>
    [Fact]
    public async Task Concert_DetailsStayPubliclyReadableAcrossTenants()
    {
        var postedConcert = fixture.SeedState.Concerts.First(c => c.DatePosted is not null);

        var thirdParty = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);
        await (await thirdParty.GetAsync($"/api/Concert/{postedConcert.Id}")).ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// The current-user concert read (<c>GET /concert/user/{id}</c>) is tenant-scoped: both parties read
    /// it and receive the party-only action links; a third-party tenant sees 404, not 403 — the deal
    /// never reveals its existence. The public read (<c>GET /concert/{id}</c>) carries no actions, so
    /// those party affordances never leak to the marketplace.
    /// </summary>
    [Fact]
    public async Task CurrentUserConcertRead_ScopesActionsToPartiesAndKeepsPublicReadActionFree()
    {
        // Arrange — drive FlatFee to Booked so a concert (with a frozen agreement) exists.
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/Application/{appId}/checkout");
        await venueClient.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.StripeClient.SendWebhookAsync();

        var booking = await fixture.ConcertReads.Set<BookingEntity>().FirstAsync(b => b.ApplicationId == appId);
        var concertId = (await fixture.ConcertReads.Set<ConcertEntity>().FirstAsync(c => c.BookingId == booking.Id)).Id;

        // Venue party — owner read succeeds and carries the action links.
        var venueRead = await venueClient.GetAsync($"/api/Concert/user/{concertId}");
        await venueRead.ShouldBe(HttpStatusCode.OK);
        var venueConcert = await venueRead.Content.ReadAsync<ConcertDetailsResponse>();
        Assert.NotNull(venueConcert!.Actions);
        Assert.Equal($"/api/Concert/{concertId}/agreement/pdf", venueConcert.Actions!.Agreement!.Href);
        Assert.NotNull(venueConcert.Actions.Cancel); // Booked

        // Artist party — the other side of the deal reads it too, with the agreement link.
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var artistRead = await artistClient.GetAsync($"/api/Concert/user/{concertId}");
        await artistRead.ShouldBe(HttpStatusCode.OK);
        var artistConcert = await artistRead.Content.ReadAsync<ConcertDetailsResponse>();
        Assert.NotNull(artistConcert!.Actions!.Agreement);

        // Stranger tenant — the deal document does not exist for them (404, not 403).
        var stranger = fixture.CreateClient(fixture.SeedState.VenueManager2);
        await (await stranger.GetAsync($"/api/Concert/user/{concertId}")).ShouldBe(HttpStatusCode.NotFound);

        // Public marketplace read — same concert, but no owner affordances leak.
        var publicRead = await stranger.GetAsync($"/api/Concert/{concertId}");
        await publicRead.ShouldBe(HttpStatusCode.OK);
        var publicConcert = await publicRead.Content.ReadAsync<ConcertDetailsResponse>();
        Assert.Null(publicConcert!.Actions);
    }
}
