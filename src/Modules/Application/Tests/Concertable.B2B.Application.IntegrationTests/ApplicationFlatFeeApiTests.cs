using System.Net;
using Concertable.B2B.Application.Api.Responses;
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
public sealed class ApplicationFlatFeeApiTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationFlatFeeApiTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task AcceptCheckout_ShouldReturnHoldSessionWithChargeLabels()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.PostAsync(
            $"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");

        await response.ShouldBe(HttpStatusCode.OK);
        var checkout = await response.Content.ReadAsync<Checkout>();
        Assert.NotNull(checkout);
        Assert.Equal(CheckoutLabels.Charge, checkout.Labels);
        Assert.StartsWith("pi_", checkout.Session.ClientSecret);
        Assert.IsType<FlatPayment>(checkout.Amount);
        Assert.NotEmpty(checkout.Session.ClientSecret);
    }

    [Fact]
    public async Task ApplyCheckout_ShouldReturn400_WhenContractDoesNotSupportApplyTimeCheckout()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.PostAsync(
            $"/api/application/opportunity/{fixture.SeedState.FlatFeeApp.OpportunityId}/checkout");

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Apply_ShouldCreateStandardApplication_WithoutPaymentMethod()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var opportunityResponse = await venueClient.PostAsync(
            "/api/opportunity",
            BuildOpportunityRequest(
                new FlatFeeDealDto { PaymentMethod = PaymentMethod.Cash, Fee = 500m }));
        await opportunityResponse.ShouldBe(HttpStatusCode.Created);
        var opportunity = await opportunityResponse.Content.ReadAsync<OpportunityBoundaryResponse>();
        Assert.NotNull(opportunity);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var applyResponse = await artistClient.PostAsync(
            $"/api/application/{opportunity.Id}",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await applyResponse.ShouldBe(HttpStatusCode.Created);
        var application = await applyResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.NotNull(application);
        Assert.Equal($"/api/application/{application.Id}", applyResponse.Headers.Location?.OriginalString);
        var standard = await fixture.Applications
            .OfType<StandardApplication>()
            .FirstOrDefaultAsync(value => value.OpportunityId == opportunity.Id);
        Assert.NotNull(standard);
        var emails = (await fixture.GetStagedEmailsAsync())
            .Where(email => email.Subject == "Concert Application")
            .ToList();
        Assert.Equal(2, emails.Count);
        Assert.Contains(emails, email => email.To == fixture.SeedState.VenueManager1.Email);
        Assert.Contains(emails, email => email.To == fixture.SeedState.VenueManager3.Email);
        Assert.All(emails, email => Assert.Equal(
            $"{fixture.SeedState.ArtistManager1.Email} has applied to your concert opportunity",
            email.Body));
    }

    [Fact]
    public async Task Accept_ShouldReturn409_WhenAlreadyAccepted()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var firstResponse = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await firstResponse.ShouldBe(HttpStatusCode.NoContent);
        var acceptedEmail = Assert.Single(
            await fixture.GetStagedEmailsAsync(),
            email => email.Subject == "Concert Application Accepted");
        Assert.Equal(fixture.SeedState.ArtistManager1.Email, acceptedEmail.To);
        Assert.Equal(
            "Your application was accepted! A concert has been scheduled for you.",
            acceptedEmail.Body);
        await fixture.StripeClient.SendWebhookAsync();

        var response = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await response.ShouldBe(HttpStatusCode.Conflict);
        fixture.PaymentTransport.SingleCommand<CaptureEscrowCommand>();
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
