using System.Net;
using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]
public sealed class ApplicationContractConsentApiTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationContractConsentApiTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Apply_ShouldReturn400_WithoutConsent()
    {
        var opportunityId = await CreateOpportunityAsync(
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await artistClient.PostAsync(
            $"/api/application/{opportunityId}",
            new { eSignature = new { signatoryName = string.Empty } });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.False(await fixture.Applications.AnyAsync(value => value.OpportunityId == opportunityId));
    }

    [Fact]
    public async Task Apply_ShouldRecordArtistESignatureAndFingerprint()
    {
        var opportunityId = await CreateOpportunityAsync(
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);

        var application = await fixture.Applications.FirstAsync(value => value.Id == applicationId);
        Assert.NotNull(application.ArtistESignature);
        Assert.Equal(fixture.SeedState.ArtistManager1.Id, application.ArtistESignature!.UserId);
        Assert.NotEqual(default, application.ArtistESignature.AtUtc);
        Assert.Equal("Test Signatory", application.ArtistESignature.SignatoryName);
        Assert.NotNull(application.TermsFingerprint);
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WithoutConsent()
    {
        var opportunityId = await CreateOpportunityAsync(
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/application/{applicationId}/checkout");

        var response = await venueClient.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = string.Empty } });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        var application = await fixture.Applications.FirstAsync(value => value.Id == applicationId);
        Assert.Equal(ApplicationState.Applied, application.State);
        var financial = await venueClient.GetAsync(
            $"/api/application/{applicationId}/financial-operation");
        await financial.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Accept_ShouldReturn409_WhenTermsChangedSinceApply()
    {
        var opportunityId = await CreateOpportunityAsync(
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);
        await UpdateDealAsync(
            opportunityId,
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 999m });
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/application/{applicationId}/checkout");

        var response = await venueClient.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await response.ShouldBe(HttpStatusCode.Conflict);
        var application = await fixture.Applications.FirstAsync(value => value.Id == applicationId);
        Assert.Equal(ApplicationState.Applied, application.State);
        var contract = await venueClient.GetAsync($"/api/application/{applicationId}/contract");
        await contract.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptedApplication_ExposesContractHateoasLink()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        var response = await venueClient.GetAsync($"/api/application/{applicationId}");

        await response.ShouldBe(HttpStatusCode.OK);
        var application = await response.Content.ReadAsync<ApplicationResponse<VenueApplicationActions>>();
        Assert.NotNull(application);
        Assert.NotNull(application.Actions.Contract);
        Assert.Equal($"/api/application/{applicationId}/contract/pdf", application.Actions.Contract!.Href);
        Assert.Equal("GET", application.Actions.Contract.Method);
    }

    [Fact]
    public async Task PendingApplication_HasNoContractLink()
    {
        var opportunityId = await CreateOpportunityAsync(
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await artistClient.GetAsync($"/api/application/{applicationId}");

        await response.ShouldBe(HttpStatusCode.OK);
        var application = await response.Content.ReadAsync<ApplicationResponse<ArtistApplicationActions>>();
        Assert.NotNull(application);
        Assert.Null(application.Actions.Contract);
    }

    private async Task<int> CreateOpportunityAsync(DealDto deal)
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var response = await venueClient.PostAsync("/api/opportunity", BuildOpportunityRequest(deal));
        await response.ShouldBe(HttpStatusCode.Created);
        var opportunity = await response.Content.ReadAsync<OpportunityBoundaryResponse>();
        Assert.NotNull(opportunity);
        return opportunity.Id;
    }

    private async Task<int> ApplyAsync(int opportunityId)
    {
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await artistClient.PostAsync(
            $"/api/application/{opportunityId}",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await response.ShouldBe(HttpStatusCode.Created);
        var application = await response.Content.ReadAsync<ApplicationResponse<ArtistApplicationActions>>();
        Assert.NotNull(application);
        return application.Id;
    }

    private async Task UpdateDealAsync(int opportunityId, DealDto desired)
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var currentResponse = await venueClient.GetAsync(
            $"/api/venue/{fixture.SeedState.Venue.Id}/opportunities");
        await currentResponse.ShouldBe(HttpStatusCode.OK);
        var current = await currentResponse.Content.ReadAsync<IReadOnlyList<OpportunityBoundaryResponse>>();
        Assert.NotNull(current);
        var requests = current
            .Select(opportunity => new OpportunityBoundaryRequest(
                opportunity.Id,
                opportunity.StartDate,
                opportunity.EndDate,
                opportunity.Genres,
                opportunity.Id == opportunityId ? desired : opportunity.Deal))
            .ToArray();

        var updateResponse = await venueClient.PutAsync(
            $"/api/venue/{fixture.SeedState.Venue.Id}/opportunities",
            requests);

        await updateResponse.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadAsync<IReadOnlyList<OpportunityBoundaryResponse>>();
        Assert.NotNull(updated);
        var target = Assert.Single(updated, opportunity => opportunity.Id == opportunityId);
        Assert.Equal(desired, target.Deal with { Id = desired.Id });
    }

    private OpportunityBoundaryRequest BuildOpportunityRequest(DealDto deal) =>
        new(
            null,
            fixture.SeedNow.AddMonths(1),
            fixture.SeedNow.AddMonths(1).AddHours(3),
            [Genre.Rock],
            deal);

    private sealed record OpportunityBoundaryResponse(
        int Id,
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<Genre> Genres,
        DealDto Deal);

    private sealed record OpportunityBoundaryRequest(
        int? Id,
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<Genre> Genres,
        DealDto Deal);
}
