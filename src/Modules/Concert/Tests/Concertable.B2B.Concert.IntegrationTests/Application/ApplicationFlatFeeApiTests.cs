using System.Net;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Deal.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit.Abstractions;
using static Concertable.B2B.Concert.IntegrationTests.Opportunity.OpportunityRequestBuilders;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]

public sealed class ApplicationFlatFeeApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ApplicationFlatFeeApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task AcceptCheckout_ShouldReturnHoldSessionWithChargeLabels()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var checkout = await response.Content.ReadAsync<Checkout>();
        Assert.NotNull(checkout);
        Assert.Equal(CheckoutLabels.Charge, checkout!.Labels);
        Assert.StartsWith("pi_", checkout.Session.ClientSecret);
        Assert.IsType<FlatPayment>(checkout.Amount);
        Assert.NotEmpty(checkout.Session.ClientSecret);
    }

    [Fact]
    public async Task ApplyCheckout_ShouldReturn400_WhenContractDoesNotSupportApplyTimeCheckout()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/application/opportunity/{fixture.SeedState.FlatFeeApp.OpportunityId}/checkout");

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Apply_ShouldCreateStandardApplication_WithoutPaymentMethod()
    {
        // Arrange — venue manager creates a fresh FlatFee opportunity
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var oppResponse = await venueClient.PostAsync("/api/opportunity",
            BuildRequest(new FlatFeeDeal { PaymentMethod = PaymentMethod.Cash, Fee = 500 }, fixture.SeedNow));
        var opportunity = await oppResponse.Content.ReadAsync<OpportunityResponse>();

        // Act — artist applies directly with no payment method
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var applyResponse = await artistClient.PostAsync($"/api/application/{opportunity!.Id}", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert — 201 Created, a StandardApplication row was created
        await applyResponse.ShouldBe(HttpStatusCode.Created);
        var application = await applyResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.NotNull(application);
        Assert.Equal($"/api/application/{application.Id}", applyResponse.Headers.Location?.OriginalString);
        var standard = await fixture.ConcertReads.Set<ApplicationEntity>()
            .OfType<StandardApplication>()
            .FirstOrDefaultAsync(a => a.OpportunityId == opportunity.Id);
        Assert.NotNull(standard);
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WhenAlreadyAccepted()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");

        // Act
        var firstResponse = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await firstResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();
        var response = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
        fixture.PaymentTransport.SingleCommand<CaptureEscrowCommand>();
    }

    [Fact]
    public async Task Accept_ShouldConfirmBookingAndCreateDraftConcertAndNotifyArtistAndVenueAndHoldEscrow()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var applicationResponse = await client.GetAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}");
        await applicationResponse.ShouldBe(HttpStatusCode.OK);
        var application = await applicationResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Accepted, application!.Status);

        var concertResponse = await client.GetAsync($"/api/concert/application/{fixture.SeedState.FlatFeeApp.Id}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<MyDetailsResponse>();
        Assert.NotNull(concert);
        Assert.Null(concert.DatePosted);
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
        var notifiedUserIds = fixture.NotificationService.DraftCreated.Select(n => n.UserId).ToList();
        Assert.Contains(fixture.SeedState.ArtistManager1.Id.ToString(), notifiedUserIds);
        Assert.Contains(fixture.SeedState.VenueManager1.Id.ToString(), notifiedUserIds);
        Assert.All(fixture.NotificationService.DraftCreated, n => Assert.NotNull(n.Payload));

        var booking = await fixture.ConcertReads.Set<BookingEntity>().FirstAsync(b => b.ApplicationId == fixture.SeedState.FlatFeeApp.Id);
        var venueTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.VenueManager1.Id).Id;
        var artistTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.ArtistManager1.Id).Id;
        var command = fixture.PaymentTransport.SingleCommand<CaptureEscrowCommand>();
        Assert.Equal(booking.Id, command.BookingId);
        Assert.Equal(venueTenantId, command.PayerId);
        Assert.Equal(artistTenantId, command.PayeeId);
        Assert.Equal((long)(fixture.SeedState.FlatFeeAppDeal.Fee * 100), command.AmountMinor);
    }

    [Fact]
    public async Task Accept_ShouldIgnoreDuplicateWebhookEvent()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
    }

    [Fact]
    public async Task Accept_ShouldNotConfirmBooking_WhenWebhookFails()
    {
        // Arrange
        fixture.CreateClient(fixture.SeedState.VenueManager1, o => o.UseFailingStripe());
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var applicationResponse = await client.GetAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}");
        await applicationResponse.ShouldBe(HttpStatusCode.OK);
        var application = await applicationResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Accepted, application!.Status);
        Assert.Empty(fixture.NotificationService.DraftCreated);
    }

    [Fact]
    public async Task Accept_ShouldRejectAndNotCreateDraft_WhenPaymentFails()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        await response.ShouldBe(HttpStatusCode.NoContent);
        await fixture.RejectLatestFinancialOperationAsync();
        var application = await fixture.ConcertReads.Set<ApplicationEntity>()
            .AsNoTracking()
            .SingleAsync(value => value.Id == fixture.SeedState.FlatFeeApp.Id);
        Assert.Equal(LifecycleState.PaymentFailed, application.State);
        var draft = await fixture.ConcertReads.Set<ConcertEntity>().FirstOrDefaultAsync(c => c.ApplicationId == fixture.SeedState.FlatFeeApp.Id);
        Assert.Null(draft);
        Assert.Empty(fixture.NotificationService.DraftCreated);
    }
}
