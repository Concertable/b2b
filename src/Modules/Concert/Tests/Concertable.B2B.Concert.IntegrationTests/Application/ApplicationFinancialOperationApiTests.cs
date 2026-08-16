using System.Net;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Payment.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]
public sealed class ApplicationFinancialOperationApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ApplicationFinancialOperationApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Get_Returns404BeforeFinancialOperationStarts()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync(
            $"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/financial-operation");

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_MapsPendingAndRejectedAcceptanceOperation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/Application/{applicationId}/checkout");
        var accept = await client.PostAsync(
            $"/api/Application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await accept.ShouldBe(HttpStatusCode.NoContent);
        var command = Assert.Single(
            await fixture.PaymentTransport.WaitForCommandsAsync<CaptureEscrowCommand>(1));

        var pendingResponse = await client.GetAsync(
            $"/api/Application/{applicationId}/financial-operation");
        await pendingResponse.ShouldBe(HttpStatusCode.OK);
        var pending = await pendingResponse.Content.ReadAsync<FinancialOperationResponse>();
        Assert.Equal(command.OperationId, pending!.OperationId);
        Assert.Equal(LifecycleState.Accepted, pending.Status);
        Assert.Null(pending.FailureCode);
        Assert.Null(pending.FailureMessage);

        await fixture.RejectLatestFinancialOperationAsync();

        var rejectedResponse = await client.GetAsync(
            $"/api/Application/{applicationId}/financial-operation");
        await rejectedResponse.ShouldBe(HttpStatusCode.OK);
        var rejected = await rejectedResponse.Content.ReadAsync<FinancialOperationResponse>();
        Assert.Equal(command.OperationId, rejected!.OperationId);
        Assert.Equal(LifecycleState.PaymentFailed, rejected.Status);
        Assert.Equal("card_declined", rejected.FailureCode);
        Assert.Equal("Card was declined", rejected.FailureMessage);
    }

    private sealed record FinancialOperationResponse(
        Guid OperationId,
        LifecycleState Status,
        string? FailureCode,
        string? FailureMessage);
}
