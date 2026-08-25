using System.Net;
using System.Text.Json;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]

public sealed class ApplicationApiTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationApiTests(ApplicationApiFixture fixture, ITestOutputHelper output)
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

    [Fact]
    public async Task CurrentLists_IncludeApplicationsForInProgressOpportunities()
    {
        var application = fixture.SeedState.InProgressApplication;
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager3);
        venueClient.DefaultRequestHeaders.Add(
            TenantHeaders.TenantId,
            application.VenueTenantId.ToString());
        var venueResponse = await venueClient
            .GetAsync("/api/Application/venue/current");
        var artistResponse = await fixture.CreateClient(fixture.SeedState.ArtistManager1)
            .GetAsync("/api/Application/artist/current");

        await venueResponse.ShouldBe(HttpStatusCode.OK);
        await artistResponse.ShouldBe(HttpStatusCode.OK);
        var venueApplications = await venueResponse.Content.ReadAsync<JsonElement>();
        var artistApplications = await artistResponse.Content.ReadAsync<JsonElement>();
        Assert.Contains(venueApplications.EnumerateArray(), item => item.GetProperty("id").GetInt32() == application.Id);
        Assert.Contains(artistApplications.EnumerateArray(), item => item.GetProperty("id").GetInt32() == application.Id);
    }

    #region Eligibility

    [Fact]
    public async Task CanApply_EligibleArtist_ReturnsTrue()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync(
            $"/api/application/opportunity/{fixture.SeedState.ActiveVenueHireOpportunity.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.True(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task CanApply_MissingArtist_ReturnsFalse()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync(
            $"/api/application/opportunity/{fixture.SeedState.ActiveVenueHireOpportunity.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.False(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task ConcertFacts_UpdateOwnedAvailabilityProjection()
    {
        const int concertId = int.MaxValue;
        var opportunity = fixture.SeedState.ActiveVenueHireOpportunity;
        var artist = fixture.SeedState.Artist;
        var venue = fixture.SeedState.Venues.Single(value => value.Id == opportunity.VenueId);
        var created = new ConcertCreatedEvent(
            concertId,
            0,
            opportunity.Id,
            artist.Id,
            venue.Id,
            venue.TenantId,
            artist.TenantId,
            opportunity.Period.Start);
        await fixture.DispatchIntegrationEventAsync(
            created,
            MessageEnvelope.Create<ConcertCreatedEvent>(DateTimeOffset.UtcNow));

        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var unavailable = await client.GetAsync(
            $"/api/application/opportunity/{opportunity.Id}/eligibility");

        await unavailable.ShouldBe(HttpStatusCode.OK);
        Assert.False(await unavailable.Content.ReadAsync<bool>());
        Assert.True(await fixture.ConcertAvailabilities.AnyAsync(value => value.ConcertId == concertId));

        await fixture.DispatchIntegrationEventAsync(
            new ConcertCancelledEvent(concertId, 0, opportunity.Id),
            MessageEnvelope.Create<ConcertCancelledEvent>(DateTimeOffset.UtcNow));

        var available = await client.GetAsync(
            $"/api/application/opportunity/{opportunity.Id}/eligibility");
        await available.ShouldBe(HttpStatusCode.OK);
        Assert.True(await available.Content.ReadAsync<bool>());
        Assert.False(await fixture.ConcertAvailabilities.AnyAsync(value => value.ConcertId == concertId));
    }

    [Fact]
    public async Task CanAccept_EligibleApplication_ReturnsTrue()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync(
            $"/api/application/{fixture.SeedState.FlatFeeApp.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.True(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task CanAccept_ArtistBookedAtAnotherVenue_ReturnsFalse()
    {
        const int concertId = int.MaxValue;
        var application = fixture.SeedState.FlatFeeApp;
        var opportunity = fixture.SeedState.Opportunities.Single(value => value.Id == application.OpportunityId);
        var artist = fixture.SeedState.Artists.Single(value => value.Id == application.ArtistId);
        var otherVenue = fixture.SeedState.Venues.First(value => value.TenantId != application.VenueTenantId);
        await fixture.DispatchIntegrationEventAsync(
            new ConcertCreatedEvent(
                concertId,
                0,
                int.MaxValue,
                artist.Id,
                otherVenue.Id,
                otherVenue.TenantId,
                artist.TenantId,
                opportunity.Period.Start),
            MessageEnvelope.Create<ConcertCreatedEvent>(DateTimeOffset.UtcNow));
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync($"/api/application/{application.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.False(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task CanAccept_MissingApplication_ReturnsFalse()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/application/2147483647/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.False(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task GetByOpportunity_ForeignVenue_ReturnsForbiddenProblem()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await client.GetAsync(
            $"/api/application/opportunity/{fixture.SeedState.FlatFeeApp.OpportunityId}");

        await AssertProblemCodeAsync(
            response,
            HttpStatusCode.Forbidden,
            "application.query.opportunity_forbidden");
    }

    [Theory]
    [InlineData("/api/application/artist/pending")]
    [InlineData("/api/application/artist/recently-denied")]
    public async Task ArtistQueries_MissingArtist_ReturnForbiddenProblem(string path)
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync(path);

        await AssertProblemCodeAsync(response, HttpStatusCode.Forbidden, "application.query.missing_artist");
    }

    [Fact]
    public async Task ApplyCheckout_MissingArtist_ReturnsForbiddenProblem()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.PostAsync(
            $"/api/application/opportunity/{fixture.SeedState.ActiveVenueHireOpportunity.Id}/checkout",
            null);

        await AssertProblemCodeAsync(response, HttpStatusCode.Forbidden, "application.eligibility.missing_artist");
    }

    #endregion

    #region Accept

    [Fact]
    public async Task Accept_WhenQueuedBeforeWithdraw_WinsTheLifecycleTransition()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var artist = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        await using var applicationLock = await fixture.HoldApplicationForUpdateAsync(applicationId);
        var acceptTask = venue.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.WaitForApplicationLockWaitersAsync(1);
        var withdrawTask = artist.PostAsync(
            $"/api/application/{applicationId}/withdraw",
            (object?)null);
        await fixture.WaitForApplicationLockWaitersAsync(2);

        await applicationLock.RollbackAsync();
        var accept = await acceptTask;
        var withdraw = await withdrawTask;

        await accept.ShouldBe(HttpStatusCode.NoContent);
        await withdraw.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(
            State.Accepted,
            (await fixture.Applications.SingleAsync(value => value.Id == applicationId)).State);
    }

    [Fact]
    public async Task Accept_WhenQueuedBeforeReject_WinsTheLifecycleTransition()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await using var applicationLock = await fixture.HoldApplicationForUpdateAsync(applicationId);
        var acceptTask = venue.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.WaitForApplicationLockWaitersAsync(1);
        var rejectTask = venue.PostAsync(
            $"/api/application/{applicationId}/reject",
            (object?)null);
        await fixture.WaitForApplicationLockWaitersAsync(2);

        await applicationLock.RollbackAsync();
        var accept = await acceptTask;
        var reject = await rejectTask;

        await accept.ShouldBe(HttpStatusCode.NoContent);
        await reject.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(
            State.Accepted,
            (await fixture.Applications.SingleAsync(value => value.Id == applicationId)).State);
    }

    [Fact]
    public async Task Accept_WhenVerificationOverlapsApplicationTransition_ConfirmsBooking()
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var checkout = await client.PostAsync($"/api/application/{applicationId}/checkout");
        await checkout.ShouldBe(HttpStatusCode.OK);

        await using var applicationLock = await fixture.HoldApplicationForUpdateAsync(applicationId);
        var acceptTask = client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new
            {
                paymentMethodId = "pm_card_visa",
                eSignature = new { signatoryName = "Test Signatory" }
            });
        await fixture.WaitForApplicationLockWaitersAsync(1);

        var verificationTask = fixture.StripeClient.SendWebhookAsync();
        await fixture.WaitForApplicationLockWaitersAsync(2);
        await applicationLock.RollbackAsync();

        var accept = await acceptTask;
        await verificationTask;

        await accept.ShouldBe(HttpStatusCode.NoContent);
        var bookingResponse = await client.GetAsync($"/api/booking/application/{applicationId}");
        await bookingResponse.ShouldBe(HttpStatusCode.OK);
        var booking = await bookingResponse.Content.ReadAsync<JsonElement>();
        Assert.Equal("confirmed", booking.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Accept_ShouldReturn403_WhenNotVenueManager()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accept_ShouldReturn404_WhenCalledByDifferentVenueManager()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Apply

    [Fact]
    public async Task Apply_ShouldReturn400_WhenSameArtistReappliesAfterWithdraw()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;
        var withdraw = await client.PostAsync($"/api/application/{appId}/withdraw");
        await withdraw.ShouldBe(HttpStatusCode.NoContent);

        var response = await client.PostAsync($"/api/application/{opportunityId}", new { eSignature = new { signatoryName = "Aretha Artist" } });

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        string expectedCode)
    {
        await response.ShouldBe(statusCode);
        var problem = await response.Content.ReadAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Extensions.TryGetValue("code", out var code));
        Assert.Equal(expectedCode, code?.ToString());
    }

}
