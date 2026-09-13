using Concertable.B2B.Infrastructure.Payments;
using Concertable.B2B.Booking.Contracts;
using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Concert.Contracts.Commands;
using Concertable.Payment.Contracts;
using Concertable.Shared.Email.Application;
using Xunit.Abstractions;

namespace Concertable.B2B.Lifecycle.IntegrationTests;

[Collection("Integration")]
public sealed class FlatFeeLifecycleTests : IAsyncLifetime
{
    private readonly LifecycleApiFixture fixture;

    public FlatFeeLifecycleTests(LifecycleApiFixture fixture, ITestOutputHelper output)
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
        await fixture.PaymentSimulator.SendWebhookAsync();

        var application = await GetApplicationAsync(client, applicationId);
        Assert.Equal(ApplicationBoundaryStatus.Accepted, application.Status);
        var concertResponse = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertBoundaryResponse>();
        Assert.NotNull(concert);
        Assert.Null(concert.DatePosted);
        Assert.Equal(2, (await fixture.WaitForDraftNotificationsAsync(2)).Count);
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
        Assert.Equal(PaymentOperationReferences.EscrowType, command.Reference.OperationType);
        Assert.Equal(venueTenantId, command.PayerId);
        Assert.Equal(artistTenantId, command.PayeeId);
        Assert.Equal((long)(fixture.SeedState.FlatFeeAppDeal.Fee * 100), command.AmountMinor);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingStatus.Confirmed, financial.Status);
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

        await fixture.PaymentSimulator.SendWebhookAsync();
        await fixture.PaymentSimulator.SendWebhookAsync();

        Assert.Equal(2, (await fixture.WaitForDraftNotificationsAsync(2)).Count);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingStatus.Confirmed, financial.Status);
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

        await fixture.PaymentSimulator.SendWebhookAsync();

        var application = await GetApplicationAsync(client, applicationId);
        Assert.Equal(ApplicationBoundaryStatus.Accepted, application.Status);
        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingStatus.ConfirmationFailed, financial.Status);
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
        Assert.Equal(BookingStatus.ConfirmationFailed, financial.Status);
        var concert = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concert.ShouldBe(HttpStatusCode.NotFound);
        Assert.Empty(fixture.NotificationService.DraftCreated);
    }

    [Fact]
    public async Task Confirm_ShouldRollBackBookingConcertAndMessages_WhenEmailStagingFails()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var client = fixture.CreateClient(
            fixture.SeedState.VenueManager1,
            options => options.UseFailingEmailRendering());
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.PaymentSimulator.SendWebhookAsync());
        Assert.Equal("Email rendering failed.", failure.Message);

        var financial = await GetFinancialOperationAsync(client, applicationId);
        Assert.Equal(BookingStatus.AwaitingConfirmation, financial.Status);
        await (await client.GetAsync($"/api/concert/application/{applicationId}"))
            .ShouldBe(HttpStatusCode.NotFound);
        Assert.Equal(0, await fixture.GetOutboxMessageCountAsync<NotifyConcertDraftCreatedCommand>());
        Assert.DoesNotContain(
            await fixture.GetStagedEmailsAsync(),
            email => email.Subject.StartsWith("Booking confirmed:", StringComparison.Ordinal));
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
    private sealed record ConcertBoundaryResponse(DateTime? DatePosted);

    private enum ApplicationBoundaryStatus
    {
        Pending,
        Rejected,
        Withdrawn,
        Accepted,
        Cancelled
    }

}
