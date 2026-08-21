using System.Net;
using Concertable.Payment.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Process.IntegrationTests;

[Collection("Integration")]
public sealed class CancellationJourneyTests : IAsyncLifetime
{
    private readonly ProcessApiFixture fixture;

    public CancellationJourneyTests(ProcessApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task BookingCancellation_UpdatesApplicationActionsNotifiesArtistAndReopensOpportunity()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;
        await AcceptFlatFeeAsync(client, applicationId);

        var before = await GetApplicationAsync(client, applicationId);
        Assert.Equal(ApplicationBoundaryStatus.Accepted, before.Status);
        Assert.NotNull(before.Actions.Cancel);
        Assert.Null(before.Actions.Withdraw);
        Assert.Null(before.Actions.Reject);
        Assert.DoesNotContain(await GetOpportunitiesAsync(client), value => value.Id == opportunityId);

        var cancelResponse = await client.PostAsync(before.Actions.Cancel.Href, (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();

        var after = await GetApplicationAsync(client, applicationId);
        Assert.Equal(ApplicationBoundaryStatus.Cancelled, after.Status);
        Assert.Null(after.Actions.Cancel);
        Assert.Null(after.Actions.Withdraw);
        Assert.Null(after.Actions.Reject);
        Assert.Contains(await fixture.GetStagedEmailsAsync(), email =>
            email.To == fixture.SeedState.ArtistManager1.Email &&
            email.Subject == "Concert Application Cancelled");
        Assert.Contains(await GetOpportunitiesAsync(client), value => value.Id == opportunityId);
    }

    [Fact]
    public async Task LateCaptureAfterBookingCancellation_DoesNotCreateConcert()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.VenueHireApp.Id;
        await AcceptAsync(client, applicationId);
        var application = await GetApplicationAsync(client, applicationId);
        Assert.NotNull(application.Actions.Cancel);
        var bookingId = int.Parse(application.Actions.Cancel.Href.Split('/')[3]);

        var cancelResponse = await client.PostAsync(application.Actions.Cancel.Href, (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();
        var refunds = await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();

        Assert.Equal(2, refunds.Count(command => command.BookingId == bookingId));
        var concertResponse = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concertResponse.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConcertCancellation_ReopensOpportunity()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;
        await AcceptFlatFeeAsync(client, applicationId);
        var accepted = await GetApplicationAsync(client, applicationId);
        Assert.NotNull(accepted.Actions.Cancel);
        var bookingId = int.Parse(accepted.Actions.Cancel.Href.Split('/')[3]);
        await fixture.StripeClient.SendWebhookAsync();
        Assert.DoesNotContain(await GetOpportunitiesAsync(client), value => value.Id == opportunityId);
        var concertResponse = await client.GetAsync($"/api/concert/application/{applicationId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertBoundaryResponse>();
        Assert.NotNull(concert);

        var cancelResponse = await client.PostAsync($"/api/concert/{concert.Id}/cancel");
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        var refund = fixture.PaymentTransport.SingleCommand<RefundEscrowCommand>();
        Assert.Equal(bookingId, refund.BookingId);
        Assert.Equal(RefundReasonCodes.RequestedByCustomer, refund.Reason);

        Assert.Contains(await GetOpportunitiesAsync(client), value => value.Id == opportunityId);
    }

    private async Task<IReadOnlyList<OpportunityBoundaryResponse>> GetOpportunitiesAsync(HttpClient client)
    {
        var response = await client.GetAsync(
            $"/api/venue/{fixture.SeedState.Venue.Id}/opportunities");
        await response.ShouldBe(HttpStatusCode.OK);
        var opportunities = await response.Content
            .ReadAsync<IReadOnlyList<OpportunityBoundaryResponse>>();
        Assert.NotNull(opportunities);
        return opportunities;
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

    private static async Task AcceptFlatFeeAsync(HttpClient client, int applicationId)
    {
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        await AcceptAsync(client, applicationId);
    }

    private static async Task AcceptAsync(HttpClient client, int applicationId)
    {
        var response = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    private sealed record ApplicationBoundaryResponse(
        ApplicationBoundaryStatus Status,
        ApplicationActionsBoundaryResponse Actions);

    private sealed record ApplicationActionsBoundaryResponse(
        ActionBoundaryResponse? Withdraw,
        ActionBoundaryResponse? Reject,
        ActionBoundaryResponse? Cancel);

    private sealed record ActionBoundaryResponse(string Href);
    private sealed record OpportunityBoundaryResponse(int Id);
    private sealed record ConcertBoundaryResponse(int Id);

    private enum ApplicationBoundaryStatus
    {
        Pending,
        Rejected,
        Withdrawn,
        Accepted,
        Cancelled
    }
}
