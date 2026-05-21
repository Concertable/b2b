using System.Net;
using Concertable.Concert.Application.DTOs;
using Concertable.Concert.Application.Responses;
using Concertable.Concert.Api.Responses;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Concertable.Concert.IntegrationTests.Application;

[Collection("Integration")]

public class ApplicationVersusApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ApplicationVersusApiTests(ApiFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AcceptCheckout_ShouldReturnDeferredGuaranteedDoorPaymentSession()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/{fixture.SeedData.VersusApp.Id}/checkout");

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
        var client = fixture.CreateClient(fixture.SeedData.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/Application/opportunity/{fixture.SeedData.VersusApp.OpportunityId}/checkout");

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Accept_ShouldCreateBooking_WithoutDraft()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        // Act
        var response = await client.PostAsync(
            $"/api/Application/{fixture.SeedData.VersusApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });

        // Assert — booking created but draft not created until verify webhook fires
        await response.ShouldBe(HttpStatusCode.NoContent);
        var concert = await fixture.ReadDbContext.Concerts
            .FirstOrDefaultAsync(c => c.Booking.ApplicationId == fixture.SeedData.VersusApp.Id);
        Assert.Null(concert);
    }

    [Fact]
    public async Task Accept_ShouldCreateDraftConcertAndNotifyArtistAndVenue()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedData.VersusApp.Id}/checkout");

        // Act
        var acceptResponse = await client.PostAsync($"/api/Application/{fixture.SeedData.VersusApp.Id}/accept", new { paymentMethodId = "pm_card_visa" });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var concert = await client.GetAssertAsync<ConcertDetailsResponse>($"/api/Concert/application/{fixture.SeedData.VersusApp.Id}");
        Assert.NotNull(concert);
        Assert.Null(concert.DatePosted);
        Assert.Equal(2, fixture.NotificationService.DraftCreated.Count);
        var notifiedUserIds = fixture.NotificationService.DraftCreated.Select(n => n.UserId).ToList();
        Assert.Contains(fixture.SeedData.ArtistManager1.Id.ToString(), notifiedUserIds);
        Assert.Contains(fixture.SeedData.VenueManager1.Id.ToString(), notifiedUserIds);
        Assert.All(fixture.NotificationService.DraftCreated, n => Assert.NotNull(n.Payload));
    }
}
