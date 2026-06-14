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
    private readonly ApiFixture fixture;

    public TenantScopingTests(ApiFixture fixture, ITestOutputHelper output)
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
        var applyResponse = await artistClient.PostAsync($"/api/Application/{opportunity!.Id}");
        await applyResponse.ShouldBe(HttpStatusCode.Created);

        // Assert — the row carries the frozen pair
        var application = await fixture.Applications
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
        var acceptResponse = await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/accept");
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert — application, booking and concert all carry the same pair
        var application = await fixture.Applications
            .FirstAsync(a => a.Id == fixture.SeedState.FlatFeeApp.Id);
        var booking = await fixture.Bookings
            .FirstAsync(b => b.ApplicationId == fixture.SeedState.FlatFeeApp.Id);
        var concert = await fixture.Concerts
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
}
