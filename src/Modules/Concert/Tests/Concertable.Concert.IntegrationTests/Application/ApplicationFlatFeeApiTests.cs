using System.Net;
using Concertable.Concert.Application.DTOs;
using Concertable.Concert.Application.Responses;
using Concertable.Concert.Api.Responses;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Concertable.Concert.Domain.Enums;
using Concertable.Payment.Domain;
using Concertable.Testing.Integration.B2B;

namespace Concertable.Concert.IntegrationTests.Application;

[Collection("Integration")]

public class ApplicationFlatFeeApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ApplicationFlatFeeApiTests(ApiFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AcceptCheckout_ShouldReturnHoldSessionWithChargeLabels()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/checkout");

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
        var client = fixture.CreateClient(fixture.SeedData.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/opportunity/{fixture.SeedData.FlatFeeApp.OpportunityId}/checkout");

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WhenAlreadyAccepted()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/checkout");

        // Act
        var firstResponse = await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/accept");
        await firstResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();
        var response = await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/accept");

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Accept_ShouldConfirmBookingAndCreateDraftConcertAndNotifyArtistAndVenueAndHoldEscrow()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/accept");
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var application = await client.GetAsync<ApplicationResponse>($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}");
        Assert.Equal(ApplicationStatus.Accepted, application!.Status);
        var concert = await client.GetAssertAsync<ConcertDetailsResponse>($"/api/Concert/application/{fixture.SeedData.FlatFeeApp.Id}");
        Assert.NotNull(concert);
        Assert.Null(concert.DatePosted);
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
        var notifiedUserIds = fixture.NotificationService.DraftCreated.Select(n => n.UserId).ToList();
        Assert.Contains(fixture.SeedData.ArtistManager1.Id.ToString(), notifiedUserIds);
        Assert.Contains(fixture.SeedData.VenueManager1.Id.ToString(), notifiedUserIds);
        Assert.All(fixture.NotificationService.DraftCreated, n => Assert.NotNull(n.Payload));

        var booking = await fixture.ReadDbContext.Bookings.FirstAsync(b => b.ApplicationId == fixture.SeedData.FlatFeeApp.Id);
        var escrow = await fixture.ReadDbContext.Escrows.FirstOrDefaultAsync(e => e.BookingId == booking.Id);
        Assert.NotNull(escrow);
        Assert.Equal(EscrowStatus.Held, escrow!.Status);
        Assert.NotEmpty(escrow.ChargeId);
        Assert.Equal(fixture.SeedData.VenueManager1.Id, escrow.FromUserId);
        Assert.Equal(fixture.SeedData.ArtistManager1.Id, escrow.ToUserId);
    }

    [Fact]
    public async Task Accept_ShouldIgnoreDuplicateWebhookEvent()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/accept");
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
        fixture.CreateClient(fixture.SeedData.VenueManager1, o => o.UseFailingStripe());
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/accept");
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var application = await client.GetAsync<ApplicationResponse>($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}");
        Assert.Equal(ApplicationStatus.Accepted, application!.Status);
        Assert.Empty(fixture.NotificationService.DraftCreated);
    }

    [Fact]
    public async Task Accept_ShouldNotCreateDraft_WhenPaymentFails()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1, o => o.UseFailingPayment());

        // Act
        await client.PostAsync($"/api/Application/{fixture.SeedData.FlatFeeApp.Id}/accept");

        // Assert
        var draft = await fixture.ReadDbContext.Concerts.FirstOrDefaultAsync(c => c.Booking.ApplicationId == fixture.SeedData.FlatFeeApp.Id);
        Assert.Null(draft);
    }
}
