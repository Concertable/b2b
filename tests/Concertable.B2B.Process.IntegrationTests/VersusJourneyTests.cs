using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit.Abstractions;

namespace Concertable.B2B.Process.IntegrationTests;

[Collection("Integration")]
public sealed class VersusJourneyTests : IAsyncLifetime
{
    private readonly ProcessApiFixture fixture;

    public VersusJourneyTests(ProcessApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Accept_ShouldCreateBooking_WithoutDraft()
    {
        var applicationId = fixture.SeedState.VersusApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await AcceptAsync(client, applicationId);

        await response.ShouldBe(HttpStatusCode.NoContent);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingBoundaryState.AwaitingConfirmation, financial.Status);
        var concert = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concert.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Accept_ShouldCreateDraftConcertAndNotifyArtistAndVenue()
    {
        var applicationId = fixture.SeedState.VersusApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await AcceptAsync(client, applicationId);
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.StripeClient.SendWebhookAsync();

        var concert = await GetConcertAsync(client, applicationId);
        Assert.Null(concert.DatePosted);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingBoundaryState.Confirmed, financial.Status);
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
    public async Task Accept_ShouldIgnoreDuplicateWebhookEvent()
    {
        var applicationId = fixture.SeedState.VersusApp.Id;
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
    public async Task Accept_ShouldNotCreateDraft_WhenVerifyPaymentFails()
    {
        fixture.CreateClient(fixture.SeedState.VenueManager1, options => options.UseFailingStripe());
        var applicationId = fixture.SeedState.VersusApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await AcceptAsync(client, applicationId);
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.StripeClient.SendWebhookAsync();

        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingBoundaryState.ConfirmationFailed, financial.Status);
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
        var applicationId = fixture.SeedState.VersusApp.Id;
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
        Assert.Equal(BookingBoundaryState.Confirmed, financial.Status);
        Assert.Equal(2, (await fixture.WaitForDraftNotificationsAsync(2)).Count);
    }

    private static Task<HttpResponseMessage> AcceptAsync(HttpClient client, int applicationId) =>
        client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new
            {
                eSignature = new { signatoryName = "Test Signatory" },
                paymentMethodId = "pm_card_visa"
            });

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

    private static async Task<FinancialOperationBoundaryResponse> GetFinancialOperationAsync(
        HttpClient client,
        int applicationId)
    {
        var response = await client.GetAsync(
            $"/api/booking/application/{applicationId}");
        await response.ShouldBe(HttpStatusCode.OK);
        var financial = await response.Content.ReadAsync<FinancialOperationBoundaryResponse>();
        Assert.NotNull(financial);
        return financial;
    }

    private sealed record ConcertBoundaryResponse(int Id, DateTime? DatePosted);
    private sealed record FinancialOperationBoundaryResponse(BookingBoundaryState Status);

    private enum BookingBoundaryState
    {
        AwaitingConfirmation,
        Confirmed,
        ConfirmationFailed,
        CancellationPending,
        Cancelled,
        CancellationFailed
    }
}
