using System.Net;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.State;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.State;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Concertable.Payment.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]

public sealed class ApplicationCancelApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ApplicationCancelApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private async Task<BookingEntity> AcceptFlatFeeAsync(HttpClient client)
    {
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/application/{appId}/checkout");
        var acceptResponse = await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        return await fixture.BookingDb.Set<BookingEntity>().FirstAsync(b => b.ApplicationId == appId);
    }

    private async Task<BookingEntity> AcceptVenueHireAsync(HttpClient client)
    {
        var appId = fixture.SeedState.VenueHireApp.Id;
        var acceptResponse = await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        return await fixture.BookingDb.Set<BookingEntity>().FirstAsync(b => b.ApplicationId == appId);
    }

    private async Task<ApplicationState> ApplicationStateOfAsync(int appId)
    {
        var application = await fixture.ApplicationDb.Set<ApplicationEntity>()
            .AsNoTracking()
            .FirstAsync(a => a.Id == appId);
        return application.State;
    }

    private async Task<BookingState> BookingStateOfAsync(int appId)
    {
        var booking = await fixture.BookingDb.Set<BookingEntity>()
            .AsNoTracking()
            .FirstAsync(b => b.ApplicationId == appId);
        return booking.State;
    }

    private async Task<ConcertState> ConcertStateOfAsync(int appId)
    {
        var concert = await fixture.ConcertReads.Set<ConcertEntity>()
            .AsNoTracking()
            .FirstAsync(c => c.ApplicationId == appId);
        return concert.State;
    }

    #region Cancel from Accepted

    [Fact]
    public async Task Cancel_ShouldRefundEscrowAndMarkCancelled_FromAccepted_ForFlatFee()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var booking = await AcceptFlatFeeAsync(client);

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        var refund = fixture.PaymentTransport.SingleCommand<RefundEscrowCommand>();
        Assert.Equal(booking.Id, refund.BookingId);
        Assert.Equal(RefundReasonCodes.RequestedByCustomer, refund.Reason);
        Assert.Equal(BookingState.Cancelled, await BookingStateOfAsync(appId));
        Assert.Contains(await fixture.GetStagedEmailsAsync(), e =>
            e.To == fixture.SeedState.ArtistManager1.Email && e.Subject == "Concert Application Cancelled");
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_FromAccepted_ForDoorSplit_WithNoRefund()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.DoorSplitApp.Id;
        await client.PostAsync($"/api/application/{appId}/checkout");
        var acceptResponse = await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" }, paymentMethodId = "pm_card_visa" });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync();
        Assert.Equal(BookingState.Cancelled, await BookingStateOfAsync(appId));
        Assert.Empty(fixture.EscrowClient.Holds);
    }

    [Fact]
    public async Task Withdraw_ShouldRefundEscrowAndMarkCancelled_FromAccepted()
    {
        // Arrange
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var booking = await AcceptFlatFeeAsync(venueClient);
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/withdraw", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(booking.Id, fixture.PaymentTransport.SingleCommand<RefundEscrowCommand>().BookingId);
        Assert.Equal(BookingState.Cancelled, await BookingStateOfAsync(appId));
    }

    #endregion

    #region Cancel from PaymentFailed

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_FromPaymentFailed()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        var booking = await AcceptVenueHireAsync(client);
        await fixture.SendEscrowFailedWebhookAsync(booking.Id);
        Assert.Equal(BookingState.ConfirmationFailed, await BookingStateOfAsync(appId));

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(BookingState.Cancelled, await BookingStateOfAsync(appId));
    }

    #endregion

    #region Late capture compensation

    [Fact]
    public async Task Cancel_ShouldRefundAgainAndStayCancelled_WhenEscrowCaptureLandsAfterCancel()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        var booking = await AcceptVenueHireAsync(client);
        var cancelResponse = await client.PostAsync($"/api/application/{appId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        // Act
        await fixture.StripeClient.SendWebhookAsync();
        var refunds = await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();

        // Assert
        Assert.Equal(BookingState.Cancelled, await BookingStateOfAsync(appId));
        Assert.Equal(2, refunds.Count(command => command.BookingId == booking.Id));
        var draft = await fixture.ConcertReads.Set<ConcertEntity>().FirstOrDefaultAsync(c => c.ApplicationId == appId);
        Assert.Null(draft);
    }

    #endregion

    #region Guards

    [Fact]
    public async Task Cancel_ShouldReturn409_WhenBooked()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await AcceptFlatFeeAsync(client);
        await fixture.StripeClient.SendWebhookAsync();
        Assert.Equal(ConcertState.Draft, await ConcertStateOfAsync(appId));

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(ConcertState.Draft, await ConcertStateOfAsync(appId));
    }

    [Fact]
    public async Task Cancel_ShouldReturn409_WhenStillPending()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(ApplicationState.Applied, await ApplicationStateOfAsync(appId));
    }

    [Fact]
    public async Task Cancel_ShouldReturn403_WhenCallerIsArtist()
    {
        // Arrange
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await AcceptFlatFeeAsync(venueClient);
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
        Assert.Equal(BookingState.AwaitingConfirmation, await BookingStateOfAsync(appId));
    }

    #endregion

    #region Opportunity re-opens

    [Fact]
    public async Task Cancel_ShouldReopenOpportunity()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;
        await AcceptFlatFeeAsync(client);

        var closedResponse = await client.GetAsync($"/api/venue/{fixture.SeedState.Venue.Id}/opportunities");
        var closed = await closedResponse.Content.ReadAsync<IEnumerable<OpportunityResponse>>();
        Assert.DoesNotContain(closed!, o => o.Id == opportunityId);

        // Act
        var cancelResponse = await client.PostAsync($"/api/application/{appId}/cancel", (object?)null);

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        var reopenedResponse = await client.GetAsync($"/api/venue/{fixture.SeedState.Venue.Id}/opportunities");
        var reopened = await reopenedResponse.Content.ReadAsync<IEnumerable<OpportunityResponse>>();
        Assert.Contains(reopened!, o => o.Id == opportunityId);
    }

    [Fact]
    public async Task ConcertCancel_ShouldReopenOpportunity()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;
        await AcceptFlatFeeAsync(client);
        await fixture.StripeClient.SendWebhookAsync();
        var concertResponse = await client.GetAsync($"/api/concert/application/{appId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<MyDetailsResponse>();

        // Act
        var cancelResponse = await client.PostAsync($"/api/concert/{concert!.Id}/cancel");

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        var reopenedResponse = await client.GetAsync($"/api/venue/{fixture.SeedState.Venue.Id}/opportunities");
        var reopened = await reopenedResponse.Content.ReadAsync<IEnumerable<OpportunityResponse>>();
        Assert.Contains(reopened!, o => o.Id == opportunityId);
    }

    #endregion

    #region HATEOAS

    [Fact]
    public async Task GetById_ShouldOfferCancelAndWithdraw_WhileAccepted_AndNoneOnceCancelled()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await AcceptFlatFeeAsync(client);
        var beforeResponse = await client.GetAsync($"/api/application/{appId}");
        await beforeResponse.ShouldBe(HttpStatusCode.OK);
        var before = await beforeResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Accepted, before!.Status);
        Assert.NotNull(before.Actions.Cancel);
        Assert.NotNull(before.Actions.Withdraw);
        Assert.Null(before.Actions.Reject);

        // Act
        var cancelResponse = await client.PostAsync($"/api/application/{appId}/cancel", (object?)null);

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        var afterResponse = await client.GetAsync($"/api/application/{appId}");
        await afterResponse.ShouldBe(HttpStatusCode.OK);
        var after = await afterResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Cancelled, after!.Status);
        Assert.Null(after.Actions.Cancel);
        Assert.Null(after.Actions.Withdraw);
        Assert.Null(after.Actions.Reject);
    }

    #endregion
}
