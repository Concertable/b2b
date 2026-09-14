using System.Net;
using Concertable.B2B.Application.Application.Responses;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;
using Concertable.Payment.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]
public sealed class ApplicationVenueHireApiTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationVenueHireApiTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task ApplyCheckout_ShouldReturnAuthorizeFlatPaymentSession()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.PostAsync(
            $"/api/application/opportunity/{fixture.SeedState.VenueHireApp.OpportunityId}/checkout");

        await response.ShouldBe(HttpStatusCode.OK);
        var checkout = await response.Content.ReadAsync<Checkout>();
        Assert.NotNull(checkout);
        Assert.Equal(CheckoutLabels.Charge, checkout.Labels);
        Assert.IsType<FlatPayment>(checkout.Amount);
        Assert.NotEmpty(checkout.Session.ClientSecret);
    }

    [Fact]
    public async Task AcceptCheckout_ShouldReturn400_WhenContractDoesNotSupportAcceptTimeCheckout()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.PostAsync(
            $"/api/application/{fixture.SeedState.VenueHireApp.Id}/checkout");

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApplyCheckoutThenApply_ShouldStorePaymentMethodOnApplicationEntity()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var opportunityResponse = await venueClient.PostAsync(
            "/api/opportunity",
            BuildOpportunityRequest());
        await opportunityResponse.ShouldBe(HttpStatusCode.Created);
        var opportunity = await opportunityResponse.Content.ReadAsync<OpportunityBoundaryResponse>();
        Assert.NotNull(opportunity);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var checkoutResponse = await artistClient.PostAsync(
            $"/api/application/opportunity/{opportunity.Id}/checkout");
        await checkoutResponse.ShouldBe(HttpStatusCode.OK);

        var applyResponse = await artistClient.PostAsync(
            $"/api/application/{opportunity.Id}",
            new
            {
                eSignature = new { signatoryName = "Test Signatory" }
            });

        await applyResponse.ShouldBe(HttpStatusCode.Created);
        var prepaid = await fixture.Applications
            .OfType<ApplicationEntity>()
            .FirstOrDefaultAsync(value => value.OpportunityId == opportunity.Id);
        Assert.NotNull(prepaid);
    }

    [Fact]
    public async Task Accept_ShouldReturn409_WhenAlreadyAccepted()
    {
        var applicationId = fixture.SeedState.VenueHireApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = new { eSignature = new { signatoryName = "Test Signatory" } };
        var firstResponse = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            request);
        await firstResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.PaymentSimulator.SendWebhookAsync();

        var response = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            request);

        await response.ShouldBe(HttpStatusCode.Conflict);
        fixture.PaymentTransport.SingleCommand<DepositEscrowCommand>();
    }

    [Fact]
    public async Task Apply_ShouldReturn402_WhenNoPaymentMethodIsCommitted()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var opportunityResponse = await venueClient.PostAsync(
            "/api/opportunity",
            BuildOpportunityRequest());
        await opportunityResponse.ShouldBe(HttpStatusCode.Created);
        var opportunity = await opportunityResponse.Content.ReadAsync<OpportunityBoundaryResponse>();
        Assert.NotNull(opportunity);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var applyResponse = await artistClient.PostAsync(
            $"/api/application/{opportunity.Id}",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await applyResponse.ShouldBe(HttpStatusCode.PaymentRequired);
        Assert.False(await fixture.Applications.AnyAsync(value => value.OpportunityId == opportunity.Id));
    }

    private OpportunityBoundaryRequest BuildOpportunityRequest() =>
        new(
            fixture.SeedNow.AddMonths(13),
            fixture.SeedNow.AddMonths(13).AddHours(3),
            [Genre.Rock],
            new VenueHireDealDto
            {
                PaymentMethod = PaymentMethod.Cash,
                HireFee = 250m
            });

    private sealed record OpportunityBoundaryRequest(
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<Genre> Genres,
        DealDto Deal);

    private sealed record OpportunityBoundaryResponse(int Id);
}
