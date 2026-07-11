using System.Net;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]

public sealed class ApplicationCancelApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ApplicationCancelApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private async Task<BookingEntity> AcceptFlatFeeAsync(HttpClient client)
    {
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/Application/{appId}/checkout");
        var acceptResponse = await client.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        return await fixture.ConcertReads.Set<BookingEntity>().FirstAsync(b => b.ApplicationId == appId);
    }

    private async Task<BookingEntity> AcceptVenueHireAsync(HttpClient client)
    {
        var appId = fixture.SeedState.VenueHireApp.Id;
        var acceptResponse = await client.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        return await fixture.ConcertReads.Set<BookingEntity>().FirstAsync(b => b.ApplicationId == appId);
    }

    private async Task<LifecycleState> StateOfAsync(int appId)
    {
        var application = await fixture.ConcertReads.Set<ApplicationEntity>()
            .AsNoTracking()
            .FirstAsync(a => a.Id == appId);
        return application.State;
    }

    #region Cancel from Accepted

    [Fact]
    public async Task Cancel_ShouldRefundEscrowAndMarkCancelled_FromAccepted_ForFlatFee()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var booking = await AcceptFlatFeeAsync(client);

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Contains(booking.Id, fixture.EscrowClient.Refunds);
        Assert.Equal(LifecycleState.Cancelled, await StateOfAsync(appId));
        Assert.Contains(fixture.EmailSender.Sent, e =>
            e.To == fixture.SeedState.Artist.Email && e.Subject == "Concert Application Cancelled");
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_FromAccepted_ForDoorSplit_WithNoRefund()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.DoorSplitApp.Id;
        await client.PostAsync($"/api/Application/{appId}/checkout");
        var acceptResponse = await client.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" }, paymentMethodId = "pm_card_visa" });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(LifecycleState.Cancelled, await StateOfAsync(appId));
        Assert.Empty(fixture.EscrowClient.Holds);
    }

    [Fact]
    public async Task Withdraw_ShouldRefundEscrowAndMarkCancelled_FromAccepted()
    {
        // Arrange
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var booking = await AcceptFlatFeeAsync(venueClient);
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/withdraw", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Contains(booking.Id, fixture.EscrowClient.Refunds);
        Assert.Equal(LifecycleState.Cancelled, await StateOfAsync(appId));
    }

    #endregion

    #region Cancel from PaymentFailed

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_FromPaymentFailed()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        var booking = await AcceptVenueHireAsync(client);
        await fixture.SendEscrowFailedWebhookAsync(booking.Id);
        Assert.Equal(LifecycleState.PaymentFailed, await StateOfAsync(appId));

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(LifecycleState.Cancelled, await StateOfAsync(appId));
    }

    #endregion

    #region Late capture compensation

    [Fact]
    public async Task Cancel_ShouldRefundAgainAndStayCancelled_WhenEscrowCaptureLandsAfterCancel()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        var booking = await AcceptVenueHireAsync(client);
        var cancelResponse = await client.PostAsync($"/api/Application/{appId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        // Act
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        Assert.Equal(LifecycleState.Cancelled, await StateOfAsync(appId));
        Assert.Equal(2, fixture.EscrowClient.Refunds.Count(id => id == booking.Id));
        var draft = await fixture.ConcertReads.Set<ConcertEntity>().FirstOrDefaultAsync(c => c.Booking.ApplicationId == appId);
        Assert.Null(draft);
    }

    #endregion

    #region Guards

    [Fact]
    public async Task Cancel_ShouldReturn409_WhenBooked()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await AcceptFlatFeeAsync(client);
        await fixture.StripeClient.SendWebhookAsync();
        Assert.Equal(LifecycleState.Booked, await StateOfAsync(appId));

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(LifecycleState.Booked, await StateOfAsync(appId));
    }

    [Fact]
    public async Task Cancel_ShouldReturn409_WhenStillPending()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(LifecycleState.Applied, await StateOfAsync(appId));
    }

    [Fact]
    public async Task Cancel_ShouldReturn403_WhenCallerIsArtist()
    {
        // Arrange
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await AcceptFlatFeeAsync(venueClient);
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
        Assert.Equal(LifecycleState.Accepted, await StateOfAsync(appId));
    }

    #endregion

    #region Opportunity re-opens

    [Fact]
    public async Task Cancel_ShouldReopenOpportunity()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;
        await AcceptFlatFeeAsync(client);

        var closedResponse = await client.GetAsync($"/api/Venue/{fixture.SeedState.Venue.Id}/opportunities");
        var closed = await closedResponse.Content.ReadAsync<IEnumerable<OpportunityResponse>>();
        Assert.DoesNotContain(closed!, o => o.Id == opportunityId);

        // Act
        var cancelResponse = await client.PostAsync($"/api/Application/{appId}/cancel", (object?)null);

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        var reopenedResponse = await client.GetAsync($"/api/Venue/{fixture.SeedState.Venue.Id}/opportunities");
        var reopened = await reopenedResponse.Content.ReadAsync<IEnumerable<OpportunityResponse>>();
        Assert.Contains(reopened!, o => o.Id == opportunityId);
    }

    [Fact]
    public async Task ConcertCancel_ShouldReopenOpportunity()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;
        await AcceptFlatFeeAsync(client);
        await fixture.StripeClient.SendWebhookAsync();
        var concertResponse = await client.GetAsync($"/api/Concert/application/{appId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertDetailsResponse>();

        // Act
        var cancelResponse = await client.PostAsync($"/api/Concert/{concert!.Id}/cancel");

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        var reopenedResponse = await client.GetAsync($"/api/Venue/{fixture.SeedState.Venue.Id}/opportunities");
        var reopened = await reopenedResponse.Content.ReadAsync<IEnumerable<OpportunityResponse>>();
        Assert.Contains(reopened!, o => o.Id == opportunityId);
    }

    #endregion

    #region HATEOAS

    [Fact]
    public async Task GetById_ShouldOfferCancelAndWithdraw_WhileAccepted_AndNoneOnceCancelled()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await AcceptFlatFeeAsync(client);
        var beforeResponse = await client.GetAsync($"/api/Application/{appId}");
        await beforeResponse.ShouldBe(HttpStatusCode.OK);
        var before = await beforeResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Accepted, before!.Status);
        Assert.NotNull(before.Actions.Cancel);
        Assert.NotNull(before.Actions.Withdraw);
        Assert.Null(before.Actions.Reject);

        // Act
        var cancelResponse = await client.PostAsync($"/api/Application/{appId}/cancel", (object?)null);

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        var afterResponse = await client.GetAsync($"/api/Application/{appId}");
        await afterResponse.ShouldBe(HttpStatusCode.OK);
        var after = await afterResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Cancelled, after!.Status);
        Assert.Null(after.Actions.Cancel);
        Assert.Null(after.Actions.Withdraw);
        Assert.Null(after.Actions.Reject);
    }

    #endregion
}
