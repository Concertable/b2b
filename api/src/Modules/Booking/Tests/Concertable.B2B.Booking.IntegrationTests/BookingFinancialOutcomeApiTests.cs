using System.Net;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Payment.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Booking.IntegrationTests;

[Collection("Integration")]
public sealed class BookingFinancialOutcomeApiTests : IAsyncLifetime
{
    private readonly BookingApiFixture fixture;

    public BookingFinancialOutcomeApiTests(BookingApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Get_Returns404BeforeBookingExists()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync(
            $"/api/booking/application/{fixture.SeedState.FlatFeeApp.Id}");

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_MapsPendingAndRejectedAcceptanceOperation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var accept = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await accept.ShouldBe(HttpStatusCode.NoContent);
        var command = Assert.Single(
            await fixture.PaymentTransport.WaitForCommandsAsync<CaptureEscrowCommand>(1));

        var pendingResponse = await client.GetAsync(
            $"/api/booking/application/{applicationId}");
        await pendingResponse.ShouldBe(HttpStatusCode.OK);
        var pending = await pendingResponse.Content.ReadAsync<BookingSummary>();
        Assert.Equal(command.OperationId, pending!.OperationId);
        Assert.Equal(BookingStatus.AwaitingConfirmation, pending.Status);
        Assert.Null(pending.FailureCode);
        Assert.Null(pending.FailureMessage);

        await fixture.RejectLatestFinancialOperationAsync();

        var rejectedResponse = await client.GetAsync(
            $"/api/booking/application/{applicationId}");
        await rejectedResponse.ShouldBe(HttpStatusCode.OK);
        var rejected = await rejectedResponse.Content.ReadAsync<BookingSummary>();
        Assert.Equal(command.OperationId, rejected!.OperationId);
        Assert.Equal(BookingStatus.ConfirmationFailed, rejected.Status);
        Assert.Equal("card_declined", rejected.FailureCode);
        Assert.Equal("Card was declined", rejected.FailureMessage);
    }
}
