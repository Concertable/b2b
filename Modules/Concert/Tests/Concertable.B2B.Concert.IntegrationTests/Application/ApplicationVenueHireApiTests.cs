using System.Net;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Api.Responses;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Contract.Contracts;
using Concertable.Contracts;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]

public sealed class ApplicationVenueHireApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ApplicationVenueHireApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task ApplyCheckout_ShouldReturnAuthorizeFlatPaymentSession()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/opportunity/{fixture.SeedState.VenueHireApp.OpportunityId}/checkout");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var checkout = await response.Content.ReadAsync<Checkout>();
        Assert.NotNull(checkout);
        Assert.Equal(CheckoutLabels.Charge, checkout!.Labels);
        Assert.IsType<FlatPayment>(checkout.Amount);
        Assert.NotEmpty(checkout.Session.ClientSecret);
    }

    [Fact]
    public async Task AcceptCheckout_ShouldReturn400_WhenContractDoesNotSupportAcceptTimeCheckout()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}/checkout");

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApplyCheckoutThenApply_ShouldStorePaymentMethodOnPrepaidApplication()
    {
        // Arrange — venue manager creates a fresh VenueHire opportunity
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var oppRequest = new OpportunityRequest
        {
            StartDate = DateTime.UtcNow.AddMonths(13),
            EndDate = DateTime.UtcNow.AddMonths(13).AddHours(3),
            Genres = [Genre.Rock],
            Contract = new VenueHireContract { PaymentMethod = PaymentMethod.Cash, HireFee = 250m }
        };
        var oppResponse = await venueClient.PostAsync("/api/Opportunity", oppRequest);
        var opportunity = await oppResponse.Content.ReadAsync<OpportunityResponse>();

        // Act — artist runs apply-checkout, then applies with the PM
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var checkoutResponse = await artistClient.PostAsync($"/api/Application/opportunity/{opportunity!.Id}/checkout");
        await checkoutResponse.ShouldBe(HttpStatusCode.OK);

        var applyResponse = await artistClient.PostAsync($"/api/Application/{opportunity.Id}", new { paymentMethodId = "pm_card_visa" });
        await applyResponse.ShouldBe(HttpStatusCode.Created);

        // Assert — a PrepaidApplication was created with the supplied PM
        var prepaid = await fixture.ConcertReads.Set<ApplicationEntity>()
            .OfType<PrepaidApplication>()
            .FirstOrDefaultAsync(a => a.OpportunityId == opportunity.Id);
        Assert.NotNull(prepaid);
        Assert.Equal("pm_card_visa", prepaid!.PaymentMethodId);
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WhenAlreadyAccepted()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        await client.PostAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}/accept");
        await fixture.StripeClient.SendWebhookAsync();
        var response = await client.PostAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}/accept");

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.Single(fixture.EscrowClient.Holds); // rejected second accept must not place a second hold
    }

    [Fact]
    public async Task Accept_ShouldConfirmBookingAndCreateDraftConcertAndNotifyArtistAndVenueAndHoldEscrow()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        await client.PostAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}/accept");
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var applicationResponse = await client.GetAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}");
        await applicationResponse.ShouldBe(HttpStatusCode.OK);
        var application = await applicationResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Accepted, application!.Status);

        var concertResponse = await client.GetAsync($"/api/Concert/application/{fixture.SeedState.VenueHireApp.Id}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertDetailsResponse>();
        Assert.NotNull(concert);
        Assert.Null(concert.DatePosted);
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
        var notifiedUserIds = fixture.NotificationService.DraftCreated.Select(n => n.UserId).ToList();
        Assert.Contains(fixture.SeedState.ArtistManager1.Id.ToString(), notifiedUserIds);
        Assert.Contains(fixture.SeedState.VenueManager1.Id.ToString(), notifiedUserIds);
        Assert.All(fixture.NotificationService.DraftCreated, n => Assert.NotNull(n.Payload));

        var booking = await fixture.ConcertReads.Set<BookingEntity>().FirstAsync(b => b.ApplicationId == fixture.SeedState.VenueHireApp.Id);
        var artistTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.ArtistManager1.Id).Id;
        var venueTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.VenueManager1.Id).Id;
        var hold = Assert.Single(fixture.EscrowClient.Holds, h => h.BookingId == booking.Id); // exactly one hold — no double-charge
        Assert.Equal(artistTenantId, hold.PayerId);
        Assert.Equal(venueTenantId, hold.PayeeId);
        Assert.Equal(fixture.SeedState.VenueHireAppContract.HireFee, hold.Amount);
    }

    [Fact]
    public async Task Accept_ShouldIgnoreDuplicateWebhookEvent()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        await client.PostAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}/accept");
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

        // Act
        await client.PostAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}/accept");
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var applicationResponse = await client.GetAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}");
        await applicationResponse.ShouldBe(HttpStatusCode.OK);
        var application = await applicationResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Accepted, application!.Status);
        Assert.Empty(fixture.NotificationService.DraftCreated);
    }

    [Fact]
    public async Task Accept_ShouldRejectAndNotCreateDraft_WhenPaymentFails()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1, o => o.UseFailingPayment());

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}/accept");

        // Assert — a failed hold rejects the accept, leaves it un-accepted, posts no concert, notifies nobody
        await response.ShouldBe(HttpStatusCode.BadRequest);
        var application = await (await client.GetAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}")).Content.ReadAsync<ApplicationResponse>();
        Assert.NotEqual(ApplicationStatus.Accepted, application!.Status);
        var draft = await fixture.ConcertReads.Set<ConcertEntity>().FirstOrDefaultAsync(c => c.Booking.ApplicationId == fixture.SeedState.VenueHireApp.Id);
        Assert.Null(draft);
        Assert.Empty(fixture.NotificationService.DraftCreated);
    }

    [Fact]
    public async Task Apply_ShouldCreatePrepaidApplication_WithoutCheckout()
    {
        // Arrange — venue manager creates a fresh VenueHire opportunity
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var oppRequest = new OpportunityRequest
        {
            StartDate = DateTime.UtcNow.AddMonths(13),
            EndDate = DateTime.UtcNow.AddMonths(13).AddHours(3),
            Genres = [Genre.Rock],
            Contract = new VenueHireContract { PaymentMethod = PaymentMethod.Cash, HireFee = 250m }
        };
        var oppResponse = await venueClient.PostAsync("/api/Opportunity", oppRequest);
        var opportunity = await oppResponse.Content.ReadAsync<OpportunityResponse>();

        // Act — artist applies directly with a payment method (no prior /checkout call)
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var applyResponse = await artistClient.PostAsync($"/api/Application/{opportunity!.Id}", new { paymentMethodId = "pm_card_visa" });

        // Assert — 201 Created, PrepaidApplication row created with stored PM
        await applyResponse.ShouldBe(HttpStatusCode.Created);
        var prepaid = await fixture.ConcertReads.Set<ApplicationEntity>()
            .OfType<PrepaidApplication>()
            .FirstOrDefaultAsync(a => a.OpportunityId == opportunity.Id);
        Assert.NotNull(prepaid);
        Assert.Equal("pm_card_visa", prepaid!.PaymentMethodId);
    }
}
