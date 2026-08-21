using System.Net;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Responses;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]
public sealed class ApplicationVersusApiTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationVersusApiTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task AcceptCheckout_ShouldReturnDeferredGuaranteedDoorPaymentSession()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.PostAsync(
            $"/api/application/{fixture.SeedState.VersusApp.Id}/checkout");

        await response.ShouldBe(HttpStatusCode.OK);
        var checkout = await response.Content.ReadAsync<Checkout>();
        Assert.NotNull(checkout);
        Assert.Equal(CheckoutLabels.Settlement, checkout.Labels);
        Assert.IsType<GuaranteedDoorPayment>(checkout.Amount);
        Assert.NotEmpty(checkout.Session.ClientSecret);
    }

    [Fact]
    public async Task ApplyCheckout_ShouldReturn400_WhenContractDoesNotSupportApplyTimeCheckout()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.PostAsync(
            $"/api/application/opportunity/{fixture.SeedState.VersusApp.OpportunityId}/checkout");

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Apply_ShouldCreateStandardApplication_WithoutPaymentMethod()
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

        await applyResponse.ShouldBe(HttpStatusCode.Created);
        var standard = await fixture.Applications
            .OfType<StandardApplication>()
            .FirstOrDefaultAsync(value => value.OpportunityId == opportunity.Id);
        Assert.NotNull(standard);
    }

    [Fact]
    public async Task Accept_ShouldReturn409_WhenAlreadyAccepted()
    {
        var applicationId = fixture.SeedState.VersusApp.Id;
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = new
        {
            eSignature = new { signatoryName = "Test Signatory" },
            paymentMethodId = "pm_card_visa"
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

    private OpportunityBoundaryRequest BuildOpportunityRequest() =>
        new(
            fixture.SeedNow.AddMonths(1),
            fixture.SeedNow.AddMonths(1).AddHours(3),
            [Genre.Rock],
            new VersusDealDto
            {
                PaymentMethod = PaymentMethod.Cash,
                Guarantee = 200m,
                ArtistDoorPercent = 60m
            });

    private sealed record OpportunityBoundaryRequest(
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<Genre> Genres,
        DealDto Deal);

    private sealed record OpportunityBoundaryResponse(int Id);
}
