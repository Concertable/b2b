using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Payment.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Process.IntegrationTests;

[Collection("Integration")]
public sealed class VenueHireJourneyTests : IAsyncLifetime
{
    private readonly ProcessApiFixture fixture;

    public VenueHireJourneyTests(ProcessApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Accept_ShouldConfirmBookingAndCreateDraftConcertAndNotifyArtistAndVenueAndHoldEscrow()
    {
        var applicationId = fixture.SeedState.VenueHireApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var acceptResponse = await AcceptAsync(client, applicationId);
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var accepted = await GetApplicationAsync(client, applicationId);
        Assert.Equal(ApplicationBoundaryStatus.Accepted, accepted.Status);
        Assert.NotNull(accepted.Actions.Cancel);
        var bookingId = int.Parse(accepted.Actions.Cancel.Href.Split('/')[3]);
        await fixture.StripeClient.SendWebhookAsync();

        var concert = await GetConcertAsync(client, applicationId);
        Assert.Null(concert.DatePosted);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingBoundaryState.Confirmed, financial.Status);
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
        var notifiedUserIds = fixture.NotificationService.DraftCreated
            .Select(notification => notification.UserId)
            .ToList();
        Assert.Contains(fixture.SeedState.ArtistManager1.Id.ToString(), notifiedUserIds);
        Assert.Contains(fixture.SeedState.VenueManager1.Id.ToString(), notifiedUserIds);
        Assert.All(fixture.NotificationService.DraftCreated, notification =>
            Assert.NotNull(notification.Payload));
        var artistTenantId = fixture.SeedState.Tenants
            .Single(tenant => tenant.CreatedByUserId == fixture.SeedState.ArtistManager1.Id)
            .Id;
        var venueTenantId = fixture.SeedState.Tenants
            .Single(tenant => tenant.CreatedByUserId == fixture.SeedState.VenueManager1.Id)
            .Id;
        var command = fixture.PaymentTransport.SingleCommand<DepositEscrowCommand>();
        Assert.Equal(bookingId, command.BookingId);
        Assert.Equal(artistTenantId, command.PayerId);
        Assert.Equal(venueTenantId, command.PayeeId);
        Assert.Equal(
            (long)(fixture.SeedState.VenueHireAppDeal.HireFee * 100),
            command.AmountMinor);
    }

    [Fact]
    public async Task Accept_ShouldIgnoreDuplicateWebhookEvent()
    {
        var applicationId = fixture.SeedState.VenueHireApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var acceptResponse = await AcceptAsync(client, applicationId);
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.StripeClient.SendWebhookAsync();
        var firstConcert = await GetConcertAsync(client, applicationId);
        await fixture.StripeClient.SendWebhookAsync();
        var redeliveredConcert = await GetConcertAsync(client, applicationId);

        Assert.Equal(firstConcert.Id, redeliveredConcert.Id);
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
    }

    [Fact]
    public async Task Accept_ShouldNotConfirmBooking_WhenWebhookFails()
    {
        fixture.CreateClient(fixture.SeedState.VenueManager1, options => options.UseFailingStripe());
        var applicationId = fixture.SeedState.VenueHireApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var acceptResponse = await AcceptAsync(client, applicationId);
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
    public async Task Accept_ShouldRejectAndNotCreateDraft_WhenPaymentFails()
    {
        var applicationId = fixture.SeedState.VenueHireApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var response = await AcceptAsync(client, applicationId);
        await response.ShouldBe(HttpStatusCode.NoContent);

        await fixture.RejectLatestFinancialOperationAsync();

        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingBoundaryState.ConfirmationFailed, financial.Status);
        var concert = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concert.ShouldBe(HttpStatusCode.NotFound);
        Assert.Empty(fixture.NotificationService.DraftCreated);
    }

    private static Task<HttpResponseMessage> AcceptAsync(HttpClient client, int applicationId) =>
        client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });

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

    private sealed record ApplicationBoundaryResponse(
        ApplicationBoundaryStatus Status,
        ApplicationActionsBoundaryResponse Actions);

    private sealed record ApplicationActionsBoundaryResponse(ActionBoundaryResponse? Cancel);
    private sealed record ActionBoundaryResponse(string Href);
    private sealed record ConcertBoundaryResponse(int Id, DateTime? DatePosted);
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
