using Concertable.B2B.Booking.Contracts;
using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit.Abstractions;

namespace Concertable.B2B.Lifecycle.IntegrationTests;

[Collection("Integration")]
public sealed class DoorSplitLifecycleTests : IAsyncLifetime
{
    private readonly LifecycleApiFixture fixture;

    public DoorSplitLifecycleTests(LifecycleApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Accept_ShouldCreateDeferredBooking_WithoutDraft()
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await AcceptAsync(client, applicationId);

        await response.ShouldBe(HttpStatusCode.NoContent);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingStatus.AwaitingConfirmation, financial.Status);
        var concert = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concert.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Accept_ShouldCreateDraftConcertAndNotifyArtistAndVenue()
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");

        var acceptResponse = await AcceptAsync(client, applicationId);
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        var application = await GetApplicationAsync(client, applicationId);
        Assert.Equal(ApplicationBoundaryStatus.Accepted, application.Status);
        var concert = await GetConcertAsync(client, applicationId);
        Assert.Null(concert.DatePosted);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingStatus.Confirmed, financial.Status);
        Assert.Equal(2, (await fixture.WaitForDraftNotificationsAsync(2)).Count);
        var notifiedUserIds = fixture.NotificationService.DraftCreated
            .Select(notification => notification.UserId)
            .ToList();
        Assert.Contains(fixture.SeedState.ArtistManager1.Id.ToString(), notifiedUserIds);
        Assert.Contains(fixture.SeedState.VenueManager1.Id.ToString(), notifiedUserIds);
        Assert.All(fixture.NotificationService.DraftCreated, notification =>
            Assert.NotNull(notification.Payload));
    }

    [Fact]
    public async Task Accept_ShouldCreateOneConcert_WhenWebhookEventIsRedelivered()
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await AcceptAsync(client, applicationId);
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.StripeClient.SendWebhookAsync();
        var firstConcert = await GetConcertAsync(client, applicationId);
        await fixture.StripeClient.SendWebhookAsync();
        var redeliveredConcert = await GetConcertAsync(client, applicationId);

        Assert.Equal(firstConcert.Id, redeliveredConcert.Id);
        Assert.Equal(2, (await fixture.WaitForDraftNotificationsAsync(2)).Count);
    }

    [Fact]
    public async Task Accept_ShouldNotCreateDraft_WhenFinancialConfirmationFails()
    {
        fixture.CreateClient(fixture.SeedState.VenueManager1, options => options.UseFailingStripe());
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await AcceptAsync(client, applicationId);
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.StripeClient.SendWebhookAsync();

        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingStatus.ConfirmationFailed, financial.Status);
        var concert = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concert.ShouldBe(HttpStatusCode.NotFound);
        Assert.Empty(fixture.NotificationService.DraftCreated);
        var notification = Assert.Single(
            await fixture.WaitForNotificationsAsync("VerifyPaymentFailed"));
        Assert.Equal(fixture.SeedState.VenueManager1.Id.ToString(), notification.UserId);
    }

    [Fact]
    public async Task Accept_ShouldCreateDraftConcert_WhenVerifyWebhookArrivesBeforeAccept()
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        await fixture.StripeClient.SendWebhookAsync();
        var beforeAccept = await client.GetAsync($"/api/concert/application/{applicationId}");
        await beforeAccept.ShouldBe(HttpStatusCode.NotFound);
        Assert.Empty(fixture.NotificationService.DraftCreated);

        var acceptResponse = await AcceptAsync(client, applicationId);

        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await GetConcertAsync(client, applicationId);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingStatus.Confirmed, financial.Status);
        Assert.Equal(2, (await fixture.WaitForDraftNotificationsAsync(2)).Count);
    }

    [Fact]
    public async Task Accept_ShouldRouteBookingToConfirmationFailed_WhenVerifyFailureArrivesBeforeAccept()
    {
        fixture.CreateClient(fixture.SeedState.VenueManager1, options => options.UseFailingStripe());
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        await fixture.StripeClient.SendWebhookAsync();

        var acceptResponse = await AcceptAsync(client, applicationId);

        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingStatus.ConfirmationFailed, financial.Status);
        var concert = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concert.ShouldBe(HttpStatusCode.NotFound);
        Assert.Empty(fixture.NotificationService.DraftCreated);
        Assert.Single(await fixture.WaitForNotificationsAsync("VerifyPaymentFailed"));
    }

    private static Task<HttpResponseMessage> AcceptAsync(HttpClient client, int applicationId) =>
        client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new
            {
                eSignature = new { signatoryName = "Test Signatory" },
                paymentMethodId = "pm_card_visa"
            });

    private static async Task<ApplicationBoundaryResponse> GetApplicationAsync(
        HttpClient client,
        int applicationId)
    {
        var response = await client.GetAsync($"/api/application/{applicationId}");
        await response.ShouldBe(HttpStatusCode.OK);
        var application = await response.Content.ReadAsync<ApplicationBoundaryResponse>();
        Assert.NotNull(application);
        return application;
    }

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

    private static async Task<BookingSummary> GetFinancialOperationAsync(
        HttpClient client,
        int applicationId)
    {
        var response = await client.GetAsync(
            $"/api/booking/application/{applicationId}");
        await response.ShouldBe(HttpStatusCode.OK);
        var financial = await response.Content.ReadAsync<BookingSummary>();
        Assert.NotNull(financial);
        return financial;
    }

    private sealed record ApplicationBoundaryResponse(ApplicationBoundaryStatus Status);
    private sealed record ConcertBoundaryResponse(int Id, DateTime? DatePosted);

    private enum ApplicationBoundaryStatus
    {
        Pending,
        Rejected,
        Withdrawn,
        Accepted,
        Cancelled
    }

}
