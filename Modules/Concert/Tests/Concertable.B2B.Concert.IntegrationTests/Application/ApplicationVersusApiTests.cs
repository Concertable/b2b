using System.Net;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Contract.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit.Abstractions;
using static Concertable.B2B.Concert.IntegrationTests.Opportunity.OpportunityRequestBuilders;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]

public sealed class ApplicationVersusApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ApplicationVersusApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task AcceptCheckout_ShouldReturnDeferredGuaranteedDoorPaymentSession()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedState.VersusApp.Id}/checkout");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var checkout = await response.Content.ReadAsync<Checkout>();
        Assert.NotNull(checkout);
        Assert.Equal(CheckoutLabels.Settlement, checkout!.Labels);
        Assert.IsType<GuaranteedDoorPayment>(checkout.Amount);
        Assert.NotEmpty(checkout.Session.ClientSecret);
    }

    [Fact]
    public async Task ApplyCheckout_ShouldReturn400_WhenContractDoesNotSupportApplyTimeCheckout()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/opportunity/{fixture.SeedState.VersusApp.OpportunityId}/checkout");

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Apply_ShouldCreateStandardApplication_WithoutPaymentMethod()
    {
        // Arrange — venue manager creates a fresh Versus opportunity
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var oppResponse = await venueClient.PostAsync("/api/Opportunity",
            BuildRequest(new VersusContract { PaymentMethod = PaymentMethod.Cash, Guarantee = 200, ArtistDoorPercent = 60 }));
        var opportunity = await oppResponse.Content.ReadAsync<OpportunityResponse>();

        // Act — artist applies directly with no payment method
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var applyResponse = await artistClient.PostAsync($"/api/Application/{opportunity!.Id}");

        // Assert — 201 Created, a StandardApplication row was created
        await applyResponse.ShouldBe(HttpStatusCode.Created);
        var standard = await fixture.Applications
            .OfType<StandardApplication>()
            .FirstOrDefaultAsync(a => a.OpportunityId == opportunity.Id);
        Assert.NotNull(standard);
    }

    [Fact]
    public async Task Accept_ShouldCreateBooking_WithoutDraft()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.PostAsync(
            $"/api/Application/{fixture.SeedState.VersusApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });

        // Assert — booking created but draft not created until verify webhook fires
        await response.ShouldBe(HttpStatusCode.NoContent);
        var concert = await fixture.Concerts
            .FirstOrDefaultAsync(c => c.Booking.ApplicationId == fixture.SeedState.VersusApp.Id);
        Assert.Null(concert);
    }

    [Fact]
    public async Task Accept_ShouldCreateDraftConcertAndNotifyArtistAndVenue()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedState.VersusApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync($"/api/Application/{fixture.SeedState.VersusApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var concertResponse = await client.GetAsync($"/api/Concert/application/{fixture.SeedState.VersusApp.Id}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertDetailsResponse>();
        Assert.NotNull(concert);
        Assert.Null(concert.DatePosted);
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
        var notifiedUserIds = fixture.NotificationService.DraftCreated.Select(n => n.UserId).ToList();
        Assert.Contains(fixture.SeedState.ArtistManager1.Id.ToString(), notifiedUserIds);
        Assert.Contains(fixture.SeedState.VenueManager1.Id.ToString(), notifiedUserIds);
        Assert.All(fixture.NotificationService.DraftCreated, n => Assert.NotNull(n.Payload));
    }

    [Fact]
    public async Task Accept_ShouldReturn409_WhenAlreadyAccepted()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync(
            $"/api/Application/{fixture.SeedState.VersusApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });

        // Act
        var response = await client.PostAsync(
            $"/api/Application/{fixture.SeedState.VersusApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Accept_ShouldIgnoreDuplicateWebhookEvent()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedState.VersusApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync(
            $"/api/Application/{fixture.SeedState.VersusApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
    }

    [Fact]
    public async Task Accept_ShouldNotCreateDraft_WhenVerifyPaymentFails()
    {
        // Arrange
        fixture.CreateClient(fixture.SeedState.VenueManager1, o => o.UseFailingStripe());
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedState.VersusApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync(
            $"/api/Application/{fixture.SeedState.VersusApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var application = await fixture.Applications.FirstAsync(a => a.Id == fixture.SeedState.VersusApp.Id);
        Assert.Equal(LifecycleState.PaymentFailed, application.State);
        Assert.Empty(fixture.NotificationService.DraftCreated);
        var notification = Assert.Single(fixture.NotificationService.Other, n => n.EventName == "VerifyPaymentFailed");
        Assert.Equal(fixture.SeedState.VenueManager1.Id.ToString(), notification.UserId);
    }
}
