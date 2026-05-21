using System.Net;
using Concertable.Concert.Application.DTOs;
using Concertable.Concert.Application.Responses;
using Concertable.Concert.Api.Responses;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Concertable.Concert.IntegrationTests.Application;

[Collection("Integration")]

public class ApplicationDoorSplitApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ApplicationDoorSplitApiTests(ApiFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AcceptCheckout_ShouldReturnDeferredDoorSharePaymentSession()
    {
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        var response = await client.PostAsync($"/api/Application/{fixture.SeedData.DoorSplitApp.Id}/checkout");

        await response.ShouldBe(HttpStatusCode.OK);
        var checkout = await response.Content.ReadAsync<Checkout>();
        Assert.NotNull(checkout);
        Assert.Equal(CheckoutLabels.Settlement, checkout!.Labels);
        Assert.IsType<DoorSharePayment>(checkout.Amount);
        Assert.NotEmpty(checkout.Session.ClientSecret);
    }

    [Fact]
    public async Task ApplyCheckout_ShouldReturn400_WhenContractDoesNotSupportApplyTimeCheckout()
    {
        var client = fixture.CreateClient(fixture.SeedData.ArtistManager1);

        var response = await client.PostAsync($"/api/Application/opportunity/{fixture.SeedData.DoorSplitApp.OpportunityId}/checkout");

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WhenAlreadyAccepted()
    {
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        await client.PostAsync(
            $"/api/Application/{fixture.SeedData.DoorSplitApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });

        var response = await client.PostAsync(
            $"/api/Application/{fixture.SeedData.DoorSplitApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Accept_ShouldCreateBooking_WithoutDraft()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        // Act
        var response = await client.PostAsync(
            $"/api/Application/{fixture.SeedData.DoorSplitApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });

        // Assert — booking created but draft not created until verify webhook fires
        await response.ShouldBe(HttpStatusCode.NoContent);
        var concert = await fixture.ReadDbContext.Concerts
            .FirstOrDefaultAsync(c => c.Booking.ApplicationId == fixture.SeedData.DoorSplitApp.Id);
        Assert.Null(concert);
    }

    [Fact]
    public async Task Accept_ShouldCreateDraftConcertAndNotifyArtistAndVenue()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedData.DoorSplitApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync(
            $"/api/Application/{fixture.SeedData.DoorSplitApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var application = await client.GetAsync<ApplicationResponse>(
            $"/api/Application/{fixture.SeedData.DoorSplitApp.Id}");

        Assert.Equal(ApplicationStatus.Accepted, application!.Status);

        var concert = await client.GetAssertAsync<ConcertDetailsResponse>(
            $"/api/Concert/application/{fixture.SeedData.DoorSplitApp.Id}");

        Assert.NotNull(concert);
        Assert.Null(concert.DatePosted);

        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
        var notifiedUserIds = fixture.NotificationService.DraftCreated.Select(n => n.UserId).ToList();
        Assert.Contains(fixture.SeedData.ArtistManager1.Id.ToString(), notifiedUserIds);
        Assert.Contains(fixture.SeedData.VenueManager1.Id.ToString(), notifiedUserIds);
        Assert.All(fixture.NotificationService.DraftCreated, n => Assert.NotNull(n.Payload));
    }
}