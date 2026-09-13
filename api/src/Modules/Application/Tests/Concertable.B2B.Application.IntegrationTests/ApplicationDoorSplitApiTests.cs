using System.Text.Json;
using System.Net;
using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Responses;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Contracts.Enums;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Errors;
using Concertable.Payment.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]
public sealed class ApplicationDoorSplitApiTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationDoorSplitApiTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task AcceptCheckout_ShouldReturnDeferredDoorSharePaymentSession()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.PostAsync(
            $"/api/application/{fixture.SeedState.DoorSplitApp.Id}/checkout");

        await response.ShouldBe(HttpStatusCode.OK);
        var checkout = await response.Content.ReadAsync<Checkout>();
        Assert.NotNull(checkout);
        Assert.Equal(CheckoutLabels.Settlement, checkout.Labels);
        Assert.IsType<DoorSharePayment>(checkout.Amount);
        Assert.NotEmpty(checkout.Session.ClientSecret);
    }

    [Fact]
    public async Task ApplyCheckout_ShouldReturn400_WhenContractDoesNotSupportApplyTimeCheckout()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.PostAsync(
            $"/api/application/opportunity/{fixture.SeedState.DoorSplitApp.OpportunityId}/checkout");

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Apply_ShouldCreateApplicationEntity_WithoutPaymentMethod()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var opportunityResponse = await venueClient.PostAsync(
            "/api/opportunity",
            BuildOpportunityRequest(
                new DoorSplitDealDto
                {
                    PaymentMethod = PaymentMethod.Cash,
                    ArtistDoorPercent = 70m
                }));
        await opportunityResponse.ShouldBe(HttpStatusCode.Created);
        var opportunity = await opportunityResponse.Content.ReadAsync<OpportunityBoundaryResponse>();
        Assert.NotNull(opportunity);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var applyResponse = await artistClient.PostAsync(
            $"/api/application/{opportunity.Id}",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await applyResponse.ShouldBe(HttpStatusCode.Created);
        var standard = await fixture.Applications
            .OfType<ApplicationEntity>()
            .FirstOrDefaultAsync(value => value.OpportunityId == opportunity.Id);
        Assert.NotNull(standard);
    }

    [Fact]
    public async Task Accept_ShouldReturn409_WhenAlreadyAccepted()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var request = new
        {
            eSignature = new { signatoryName = "Test Signatory" }
        };
        var firstResponse = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            request);
        await firstResponse.ShouldBe(HttpStatusCode.NoContent);

        var response = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            request);

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Accept_BeforeTheCardIsVerified_AcceptsAndWaitsForTheVerification()
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var checkout = await client.PostAsync($"/api/application/{applicationId}/checkout");
        await checkout.ShouldBe(HttpStatusCode.OK);

        var response = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await response.ShouldBe(HttpStatusCode.NoContent);
        var bookingResponse = await client.GetAsync($"/api/booking/application/{applicationId}");
        await bookingResponse.ShouldBe(HttpStatusCode.OK);
        var booking = await bookingResponse.Content.ReadAsync<JsonElement>();
        Assert.Equal("awaitingConfirmation", booking.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Verification_FromAPayerOtherThanTheVenue_IsNotRecorded()
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var reference = PaymentOperationReferences.MethodVerification(applicationId);
        await fixture.PaymentSessionClient.SetupPaymentMethodAsync(new PaymentMethodSetupRequest(
            reference,
            PaymentSessionKind.PaymentMethodVerification,
            Guid.NewGuid(),
            "mandate"));

        await fixture.DispatchIntegrationEventAsync(
            new PaymentSucceededEvent(reference, new Dictionary<string, string>()),
            MessageEnvelope.Create<PaymentSucceededEvent>(fixture.SeedNow));

        Assert.False(await fixture.PaymentVerifications.AnyAsync(verification => verification.ApplicationId == applicationId));
    }

    [Fact]
    public async Task VerificationFailure_FromAPayerOtherThanTheVenue_IsNotRecorded()
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var reference = PaymentOperationReferences.MethodVerification(applicationId);
        await fixture.PaymentSessionClient.SetupPaymentMethodAsync(new PaymentMethodSetupRequest(
            reference,
            PaymentSessionKind.PaymentMethodVerification,
            Guid.NewGuid(),
            "mandate"));
        var operationId = fixture.PaymentOperations.Latest!.OperationId!.Value;

        await fixture.DispatchIntegrationEventAsync(
            new PaymentFailedEvent(
                reference,
                "card_declined",
                "Card was declined",
                new Dictionary<string, string>
                {
                    [PaymentMetadataKeys.OperationId] = operationId.ToString("D")
                }),
            MessageEnvelope.Create<PaymentFailedEvent>(fixture.SeedNow));

        Assert.False(await fixture.PaymentVerifications.AnyAsync(verification => verification.ApplicationId == applicationId));
    }

    [Fact]
    public async Task VerificationFailure_WhenPaymentIsUnavailable_IsRetried()
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var checkout = await client.PostAsync($"/api/application/{applicationId}/checkout");
        await checkout.ShouldBe(HttpStatusCode.OK);
        var operationId = fixture.PaymentOperations.Latest!.OperationId!.Value;
        var reference = PaymentOperationReferences.MethodVerification(applicationId);
        var failed = new PaymentFailedEvent(
            reference,
            "card_declined",
            "Card was declined",
            new Dictionary<string, string>
            {
                [PaymentMetadataKeys.OperationId] = operationId.ToString("D")
            });
        var envelope = MessageEnvelope.Create<PaymentFailedEvent>(fixture.SeedNow);
        fixture.PaymentSessionClient.StatusError = new PaymentOperationError.ProviderUnavailable();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.DispatchIntegrationEventAsync(failed, envelope));

        fixture.PaymentSessionClient.StatusError = null;
        await fixture.DispatchIntegrationEventAsync(failed, envelope);

        Assert.True(await fixture.PaymentVerifications.AnyAsync(verification => verification.ApplicationId == applicationId));
    }

    private OpportunityBoundaryRequest BuildOpportunityRequest(DealDto deal) =>
        new(
            fixture.SeedNow.AddMonths(1),
            fixture.SeedNow.AddMonths(1).AddHours(3),
            [Genre.Rock],
            deal);

    private sealed record OpportunityBoundaryRequest(
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<Genre> Genres,
        DealDto Deal);

    private sealed record OpportunityBoundaryResponse(int Id);
}
