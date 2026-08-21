using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Payment.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Process.IntegrationTests;

[Collection("Integration")]
public sealed class FlatFeeJourneyTests : IAsyncLifetime
{
    private readonly ProcessApiFixture fixture;

    public FlatFeeJourneyTests(ProcessApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Accept_ShouldConfirmBookingAndCreateDraftConcertAndNotifyArtistAndVenueAndHoldEscrow()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");

        var acceptResponse = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        var application = await GetApplicationAsync(client, applicationId);
        Assert.Equal(ApplicationBoundaryStatus.Accepted, application.Status);
        var concertResponse = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertBoundaryResponse>();
        Assert.NotNull(concert);
        Assert.Null(concert.DatePosted);
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
        var notifiedUserIds = fixture.NotificationService.DraftCreated
            .Select(notification => notification.UserId)
            .ToList();
        Assert.Contains(fixture.SeedState.ArtistManager1.Id.ToString(), notifiedUserIds);
        Assert.Contains(fixture.SeedState.VenueManager1.Id.ToString(), notifiedUserIds);
        Assert.All(fixture.NotificationService.DraftCreated, notification =>
            Assert.NotNull(notification.Payload));
        var command = fixture.PaymentTransport.SingleCommand<CaptureEscrowCommand>();
        var venueTenantId = fixture.SeedState.Tenants
            .Single(tenant => tenant.CreatedByUserId == fixture.SeedState.VenueManager1.Id)
            .Id;
        var artistTenantId = fixture.SeedState.Tenants
            .Single(tenant => tenant.CreatedByUserId == fixture.SeedState.ArtistManager1.Id)
            .Id;
        Assert.True(command.BookingId > 0);
        Assert.Equal(venueTenantId, command.PayerId);
        Assert.Equal(artistTenantId, command.PayeeId);
        Assert.Equal((long)(fixture.SeedState.FlatFeeAppDeal.Fee * 100), command.AmountMinor);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingBoundaryState.Confirmed, financial.Status);
    }

    [Fact]
    public async Task Accept_ShouldIgnoreDuplicateWebhookEvent()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.StripeClient.SendWebhookAsync();
        await fixture.StripeClient.SendWebhookAsync();

        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingBoundaryState.Confirmed, financial.Status);
    }

    [Fact]
    public async Task Accept_ShouldNotConfirmBooking_WhenWebhookFails()
    {
        fixture.CreateClient(fixture.SeedState.VenueManager1, options => options.UseFailingStripe());
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.StripeClient.SendWebhookAsync();

        var application = await GetApplicationAsync(client, applicationId);
        Assert.Equal(ApplicationBoundaryStatus.Accepted, application.Status);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingBoundaryState.ConfirmationFailed, financial.Status);
        var concert = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concert.ShouldBe(HttpStatusCode.NotFound);
        Assert.Empty(fixture.NotificationService.DraftCreated);
    }

    [Fact]
    public async Task Accept_ShouldRecordConfirmationFailureAndNotCreateDraft_WhenPaymentFails()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await response.ShouldBe(HttpStatusCode.NoContent);
        await fixture.RejectLatestFinancialOperationAsync();

        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingBoundaryState.ConfirmationFailed, financial.Status);
        var concert = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concert.ShouldBe(HttpStatusCode.NotFound);
        Assert.Empty(fixture.NotificationService.DraftCreated);
    }

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

    private static async Task<FinancialOperationBoundaryResponse> GetFinancialOperationAsync(
        HttpClient client,
        int applicationId)
    {
        var response = await client.GetAsync(
            $"/api/application/{applicationId}/financial-operation");
        await response.ShouldBe(HttpStatusCode.OK);
        var financial = await response.Content.ReadAsync<FinancialOperationBoundaryResponse>();
        Assert.NotNull(financial);
        return financial;
    }

    private sealed record ApplicationBoundaryResponse(ApplicationBoundaryStatus Status);
    private sealed record ConcertBoundaryResponse(DateTime? DatePosted);
    private sealed record FinancialOperationBoundaryResponse(BookingBoundaryState Status);

    private enum ApplicationBoundaryStatus
    {
        Pending,
        Rejected,
        Withdrawn,
        Accepted,
        Cancelled
    }

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
